namespace RevenueMetrics.Application.Models;

public class RevenueSummaryResponse
{
	public DateTimeOffset From { get; set; }

	public DateTimeOffset To { get; set; }

	public decimal TotalRevenueCollected { get; set; }

	public string Currency { get; set; } = "USD";
}