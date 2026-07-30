using RevenueMetrics.Domain.Entities;

namespace RevenueMetrics.Application.Models;

public class RevenueBreakdownResponse
{
	public DateTimeOffset From { get; set; }
	public DateTimeOffset To { get; set; }
	public string Interval { get; set; } = string.Empty;
	public string Currency { get; set; } = string.Empty;
	public IReadOnlyList<RevenueBreakdownItem> Breakdown { get; set; } = new List<RevenueBreakdownItem>();
}
