using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using RevenueMetrics.Application.Exceptions;
using RevenueMetrics.Application.Interfaces;
using RevenueMetrics.Application.Models;
using RevenueMetrics.Domain.Entities;

namespace RevenueMetrics.Infrastructure.Services.SyncProviders;

public class HubSpotSyncProvider : ISyncProvider
{
	private readonly HttpClient _httpClient;

	public string SourceName => "HubSpot";

	public HubSpotSyncProvider(HttpClient httpClient, IConfiguration configuration)
	{
		_httpClient = httpClient;
		_httpClient.BaseAddress = new Uri("https://api.hubapi.com/");
		var token = configuration["HubSpot:PrivateAppToken"];
		if (!string.IsNullOrEmpty(token))
		{
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		}
	}

	public async Task<SyncResult> FetchAsync(string? currentCursor, CancellationToken cancellationToken)
	{
		var url = "crm/v3/objects/deals?properties=amount,dealstage,closedate&limit=100";
		if (!string.IsNullOrEmpty(currentCursor))
		{
			url += $"&after={currentCursor}";
		}

		var response = await _httpClient.GetAsync(url, cancellationToken);
		
		if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
		{
			var error = await response.Content.ReadAsStringAsync(cancellationToken);
			if (error.Contains("after") || error.Contains("cursor"))
			{
				throw new ExpiredCursorException("HubSpot cursor expired or invalid.");
			}
		}

		response.EnsureSuccessStatusCode();

		var content = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cancellationToken);
		var results = content?["results"]?.AsArray();

		var transactions = new List<Transaction>();

		if (results != null)
		{
			foreach (var item in results)
			{
				var id = item["id"]?.ToString() ?? Guid.NewGuid().ToString();
				var properties = item["properties"];
				var amountStr = properties?["amount"]?.ToString();
				_ = decimal.TryParse(amountStr, out var amount);
				var stage = properties?["dealstage"]?.ToString() ?? "unknown";
				
				var closeDateStr = properties?["closedate"]?.ToString();
				DateTimeOffset txDate = DateTimeOffset.UtcNow;
				if (DateTimeOffset.TryParse(closeDateStr, out var parsedDate))
				{
					txDate = parsedDate;
				}

				transactions.Add(new Transaction
				{
					Source = SourceName,
					SourceTransactionId = id,
					Amount = amount,
					Currency = "USD",
					SourceStatus = stage,
					CanonicalStatus = stage == "closedwon" ? "paid" : "pending",
					TransactionDate = txDate,
					CreatedAt = DateTimeOffset.UtcNow,
					UpdatedAt = DateTimeOffset.UtcNow,
					RawPayload = item.ToJsonString()
				});
			}
		}

		var nextCursor = content?["paging"]?["next"]?["after"]?.ToString();

		return new SyncResult
		{
			Transactions = transactions,
			NextCursor = nextCursor
		};
	}
}
