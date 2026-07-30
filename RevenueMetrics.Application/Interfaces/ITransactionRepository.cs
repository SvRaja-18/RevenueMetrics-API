using RevenueMetrics.Domain.Entities;

namespace RevenueMetrics.Application.Interfaces;

public interface ITransactionRepository
{
	Task<RevenueLedger> GetLedgerByDateRangeAsync(
		DateTimeOffset from,
		DateTimeOffset to,
		CancellationToken cancellationToken = default);
}