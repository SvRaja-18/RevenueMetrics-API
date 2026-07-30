using RevenueMetrics.Domain.Entities;

namespace RevenueMetrics.Application.Models;

public class SyncResult
{
	public List<Transaction> Transactions { get; set; } = new List<Transaction>();
	public string? NextCursor { get; set; }
}
