using RevenueMetrics.Application.Interfaces;
using RevenueMetrics.Domain.Entities;
using RevenueMetrics.Domain.Policies;

namespace RevenueMetrics.Application.Services;

public class RevenueCalculator
{
	private readonly ITransactionRepository _transactionRepository;

	public RevenueCalculator(ITransactionRepository transactionRepository)
	{
		_transactionRepository = transactionRepository;
	}

	public async Task<RevenueLedger> GetLedgerAsync(
		DateTimeOffset from,
		DateTimeOffset to,
		CancellationToken cancellationToken = default)
	{
		return await _transactionRepository.GetLedgerByDateRangeAsync(
				from,
				to,
				cancellationToken);
	}
}