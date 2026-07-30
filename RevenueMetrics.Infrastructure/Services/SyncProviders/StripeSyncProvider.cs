using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using RevenueMetrics.Application.Exceptions;
using RevenueMetrics.Application.Interfaces;
using RevenueMetrics.Application.Models;
using RevenueMetrics.Domain.Entities;

namespace RevenueMetrics.Infrastructure.Services.SyncProviders;

public class StripeSyncProvider : ISyncProvider
{
	private readonly HttpClient _httpClient;

	public string SourceName => "Stripe";

	public StripeSyncProvider(HttpClient httpClient, IConfiguration configuration)
	{
		_httpClient = httpClient;
		_httpClient.BaseAddress = new Uri("https://api.stripe.com/");
		var key = configuration["Stripe:SecretKey"];
		if (!string.IsNullOrEmpty(key))
		{
			// Stripe uses basic auth with the secret key as the username
			var authString = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{key}:"));
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);
		}
	}

	public async Task<SyncResult> FetchAsync(string? currentCursor, CancellationToken cancellationToken)
	{
		var url = "v1/charges?limit=100";
		if (!string.IsNullOrEmpty(currentCursor))
		{
			url += $"&starting_after={currentCursor}";
		}

		var response = await _httpClient.GetAsync(url, cancellationToken);
		
		if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
		{
			var error = await response.Content.ReadAsStringAsync(cancellationToken);
			if (error.Contains("starting_after") || error.Contains("cursor"))
			{
				throw new ExpiredCursorException("Stripe cursor is invalid or expired.");
			}
		}

		response.EnsureSuccessStatusCode();

		var content = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cancellationToken);
		var data = content?["data"]?.AsArray();

		var transactions = new List<Transaction>();
		string? lastId = null;

		if (data != null)
		{
			foreach (var item in data)
			{
				lastId = item["id"]?.ToString();
				if (lastId == null) continue;

				var amountCents = item["amount"]?.GetValue<long>() ?? 0;
				var currency = item["currency"]?.ToString() ?? "usd";
				var status = item["status"]?.ToString() ?? "unknown";
				var createdTs = item["created"]?.GetValue<long>() ?? 0;

				var txDate = DateTimeOffset.FromUnixTimeSeconds(createdTs);

				transactions.Add(new Transaction
				{
					Source = SourceName,
					SourceTransactionId = lastId,
					Amount = amountCents / 100m,
					Currency = currency.ToUpperInvariant(),
					SourceStatus = status,
					CanonicalStatus = status == "succeeded" ? "paid" : status,
					TransactionDate = txDate,
					CreatedAt = DateTimeOffset.UtcNow,
					UpdatedAt = DateTimeOffset.UtcNow,
					RawPayload = item.ToJsonString()
				});
			}
		}

		var hasMore = content?["has_more"]?.GetValue<bool>() ?? false;
		var nextCursor = hasMore ? lastId : null;

		return new SyncResult
		{
			Transactions = transactions,
			NextCursor = nextCursor
		};
	}
}
