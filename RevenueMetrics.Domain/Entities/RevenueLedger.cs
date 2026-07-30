using RevenueMetrics.Domain.Policies;

namespace RevenueMetrics.Domain.Entities;

public class RevenueLedger
{
	private readonly IReadOnlyList<Transaction> _transactions;

	public RevenueLedger(IEnumerable<Transaction> transactions)
	{
		_transactions = transactions.ToList().AsReadOnly();
	}

	public decimal TotalCollectedRevenue()
	{
		return _transactions
			.Where(x => RevenuePolicy.IsCollected(x.SourceStatus))
			.Sum(x => x.Amount);
	}

	public IReadOnlyList<RevenueBreakdownItem> GetCollectedRevenueBreakdown(string interval)
	{
		var collectedTransactions = _transactions
			.Where(x => RevenuePolicy.IsCollected(x.SourceStatus));

		IEnumerable<IGrouping<string, Transaction>> grouped;

		if (string.Equals(interval, "week", StringComparison.OrdinalIgnoreCase))
		{
			grouped = collectedTransactions.GroupBy(x => GetStartOfWeek(x.TransactionDate).ToString("yyyy-MM-dd"));
		}
		else
		{
			grouped = collectedTransactions.GroupBy(x => x.TransactionDate.ToString("yyyy-MM-dd"));
		}

		return grouped.Select(g => new RevenueBreakdownItem
		{
			Period = g.Key,
			CollectedRevenue = g.Sum(x => x.Amount)
		})
		.OrderBy(x => x.Period)
		.ToList()
		.AsReadOnly();
	}

	private static DateTimeOffset GetStartOfWeek(DateTimeOffset dt)
	{
		int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
		return dt.AddDays(-1 * diff).Date;
	}
}

public class RevenueBreakdownItem
{
	public string Period { get; set; } = string.Empty;
	public decimal CollectedRevenue { get; set; }
}
