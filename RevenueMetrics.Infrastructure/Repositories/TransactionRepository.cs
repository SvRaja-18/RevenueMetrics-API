using Microsoft.EntityFrameworkCore;
using RevenueMetrics.Application.Interfaces;
using RevenueMetrics.Domain.Entities;
using RevenueMetrics.Infrastructure.Persistence;

namespace RevenueMetrics.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
	private readonly AppDbContext _dbContext;

	public TransactionRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<RevenueLedger> GetLedgerByDateRangeAsync(
		DateTimeOffset from,
		DateTimeOffset to,
		CancellationToken cancellationToken = default)
	{
		var transactions = await _dbContext.Transactions
			.AsNoTracking()
			.Where(x =>
				x.TransactionDate >= from &&
				x.TransactionDate < to)
			.OrderBy(x => x.TransactionDate)
			.ToListAsync(cancellationToken);
			
		return new RevenueLedger(transactions);
	}
}