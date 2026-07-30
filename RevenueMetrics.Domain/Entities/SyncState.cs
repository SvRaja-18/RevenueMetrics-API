namespace RevenueMetrics.Domain.Entities;

public class SyncState
{
	public long Id { get; set; }
	public string SourceName { get; set; } = string.Empty;
	public string? LastCursor { get; set; }
	public DateTimeOffset? LastSyncTime { get; set; }
}
