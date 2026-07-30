using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevenueMetrics.Domain.Entities;

public class Transaction
{
	public long Id { get; set; }

	public string Source { get; set; } = string.Empty;

	public string SourceTransactionId { get; set; } = string.Empty;

	public decimal Amount { get; set; }

	public string Currency { get; set; } = string.Empty;

	public string SourceStatus { get; set; } = string.Empty;

	public string CanonicalStatus { get; set; } = string.Empty;

	public DateTimeOffset TransactionDate { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset UpdatedAt { get; set; }

	public string RawPayload { get; set; } = string.Empty;
}
