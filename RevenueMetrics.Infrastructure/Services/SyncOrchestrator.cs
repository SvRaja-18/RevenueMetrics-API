using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RevenueMetrics.Application.Exceptions;
using RevenueMetrics.Application.Interfaces;
using RevenueMetrics.Domain.Entities;
using RevenueMetrics.Infrastructure.Persistence;

namespace RevenueMetrics.Infrastructure.Services;

public class SyncOrchestrator
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<SyncOrchestrator> _logger;

	public SyncOrchestrator(IServiceProvider serviceProvider, ILogger<SyncOrchestrator> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	public async Task RunAllAsync(CancellationToken cancellationToken)
	{
		using var scope = _serviceProvider.CreateScope();
		var providers = scope.ServiceProvider.GetRequiredService<IEnumerable<ISyncProvider>>();
		
		foreach (var provider in providers)
		{
			if (cancellationToken.IsCancellationRequested) break;
			
			try
			{
				await RunProviderAsync(scope.ServiceProvider, provider, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Provider {SourceName} failed during sync. (Fault Isolation)", provider.SourceName);
			}
		}
	}

	private async Task RunProviderAsync(IServiceProvider scopedProvider, ISyncProvider provider, CancellationToken cancellationToken)
	{
		var dbContext = scopedProvider.GetRequiredService<AppDbContext>();
		
		var syncState = await dbContext.SyncStates.FirstOrDefaultAsync(x => x.SourceName == provider.SourceName, cancellationToken);
		if (syncState == null)
		{
			syncState = new SyncState { SourceName = provider.SourceName };
			dbContext.SyncStates.Add(syncState);
		}

		string? currentCursor = syncState.LastCursor;

		try
		{
			_logger.LogInformation("Starting fetch for {SourceName} with cursor {Cursor}", provider.SourceName, currentCursor);
			var result = await provider.FetchAsync(currentCursor, cancellationToken);

			foreach (var tx in result.Transactions)
			{
				var existing = await dbContext.Transactions.FirstOrDefaultAsync(x => x.Source == tx.Source && x.SourceTransactionId == tx.SourceTransactionId, cancellationToken);
				
				if (existing == null)
				{
					dbContext.Transactions.Add(tx);
				}
				else
				{
					existing.Amount = tx.Amount;
					existing.SourceStatus = tx.SourceStatus;
					existing.CanonicalStatus = tx.CanonicalStatus;
					existing.UpdatedAt = DateTimeOffset.UtcNow;
					existing.RawPayload = tx.RawPayload;
				}
			}

			syncState.LastCursor = result.NextCursor;
			syncState.LastSyncTime = DateTimeOffset.UtcNow;
			
			await dbContext.SaveChangesAsync(cancellationToken);
			_logger.LogInformation("Successfully synced {Count} records for {SourceName}. Next cursor: {NextCursor}", result.Transactions.Count, provider.SourceName, result.NextCursor);
		}
		catch (ExpiredCursorException ex)
		{
			_logger.LogWarning(ex, "Cursor expired for {SourceName}. Falling back to full backfill on next run.", provider.SourceName);
			
			syncState.LastCursor = null;
			await dbContext.SaveChangesAsync(cancellationToken);
		}
	}
}
