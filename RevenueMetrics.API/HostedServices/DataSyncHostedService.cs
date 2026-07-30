using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RevenueMetrics.Infrastructure.Services;

namespace RevenueMetrics.API.HostedServices;

public class DataSyncHostedService : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<DataSyncHostedService> _logger;

	public DataSyncHostedService(IServiceProvider serviceProvider, ILogger<DataSyncHostedService> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("DataSyncHostedService is starting.");

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using var scope = _serviceProvider.CreateScope();
				var orchestrator = scope.ServiceProvider.GetRequiredService<SyncOrchestrator>();
				
				await orchestrator.RunAllAsync(stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogCritical(ex, "Fatal error inside DataSyncHostedService loop.");
			}

			await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
		}
		
		_logger.LogInformation("DataSyncHostedService is stopping.");
	}
}
