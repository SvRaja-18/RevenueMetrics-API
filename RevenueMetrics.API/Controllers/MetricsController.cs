using Microsoft.AspNetCore.Mvc;
using RevenueMetrics.Application.Models;
using RevenueMetrics.Application.Services;

namespace RevenueMetrics.API.Controllers;

[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
	private readonly RevenueCalculator _revenueCalculator;

	public MetricsController(RevenueCalculator revenueCalculator)
	{
		_revenueCalculator = revenueCalculator;
	}

	[HttpGet("revenue")]
	public async Task<ActionResult<RevenueSummaryResponse>> GetRevenue(
		[FromQuery] DateTimeOffset from,
		[FromQuery] DateTimeOffset to,
		CancellationToken cancellationToken)
	{
		if (from >= to)
		{
			return BadRequest("The 'from' date must be earlier than the 'to' date.");
		}

		var ledger = await _revenueCalculator.GetLedgerAsync(
			from,
			to,
			cancellationToken);

		var response = new RevenueSummaryResponse
		{
			From = from,
			To = to,
			TotalRevenueCollected = ledger.TotalCollectedRevenue(),
			Currency = "USD"
		};

		return Ok(response);
	}

	[HttpGet("revenue/breakdown")]
	public async Task<ActionResult<RevenueBreakdownResponse>> GetRevenueBreakdown(
		[FromQuery] DateTimeOffset from,
		[FromQuery] DateTimeOffset to,
		[FromQuery] string interval = "day",
		CancellationToken cancellationToken = default)
	{
		if (from >= to)
		{
			return BadRequest("The 'from' date must be earlier than the 'to' date.");
		}

		var ledger = await _revenueCalculator.GetLedgerAsync(
			from,
			to,
			cancellationToken);

		var response = new RevenueBreakdownResponse
		{
			From = from,
			To = to,
			Interval = interval,
			Currency = "USD",
			Breakdown = ledger.GetCollectedRevenueBreakdown(interval)
		};

		return Ok(response);
	}
}