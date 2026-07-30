using RevenueMetrics.Application.Models;

namespace RevenueMetrics.Application.Interfaces;

public interface ISyncProvider
{
	string SourceName { get; }
	Task<SyncResult> FetchAsync(string? currentCursor, CancellationToken cancellationToken);
}
