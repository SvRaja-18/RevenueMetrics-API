using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using RevenueMetrics.Application.Exceptions;
using RevenueMetrics.Application.Interfaces;
using RevenueMetrics.Application.Models;
using RevenueMetrics.Domain.Entities;

namespace RevenueMetrics.Infrastructure.Services.SyncProviders;

public class GoogleCalendarSyncProvider : ISyncProvider
{
	private readonly string _calendarId;
	private readonly CalendarService _calendarService;

	private readonly string _credentialsPath;

	public string SourceName => "GoogleCalendar";

	public GoogleCalendarSyncProvider(IConfiguration configuration)
	{
		_calendarId = configuration["GoogleCalendar:CalendarId"] ?? "primary";
		_credentialsPath = configuration["GoogleCalendar:CredentialsPath"] ?? "credentials.json";

		string[] scopes = { CalendarService.Scope.CalendarReadonly };
		
		UserCredential credential;
		
		if (File.Exists(_credentialsPath))
		{
			using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read))
			{
				// Ensure token.json goes next to the credentials file
				string credDir = Path.GetDirectoryName(_credentialsPath) ?? ".";
				string tokenPath = Path.Combine(credDir, "token.json");
				
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.FromStream(stream).Secrets,
					scopes,
					"user",
					CancellationToken.None,
					new FileDataStore(tokenPath, true)).Result;
			}

			_calendarService = new CalendarService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "RevenueMetrics",
			});
		}
		else
		{
			// Fallback if credentials don't exist yet, to prevent app crash on startup
			_calendarService = new CalendarService(new BaseClientService.Initializer()
			{
				ApplicationName = "RevenueMetrics",
			});
		}
	}

	public async Task<SyncResult> FetchAsync(string? currentCursor, CancellationToken cancellationToken)
	{
		if (!File.Exists("credentials.json"))
		{
			// Return empty if no credentials
			return new SyncResult { Transactions = new List<Transaction>(), NextCursor = currentCursor };
		}

		var request = _calendarService.Events.List(_calendarId);
		request.MaxResults = 250;
		
		if (!string.IsNullOrEmpty(currentCursor))
		{
			request.SyncToken = currentCursor;
		}

		Google.Apis.Calendar.v3.Data.Events events = null;
		try
		{
			events = await request.ExecuteAsync(cancellationToken);
		}
		catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Gone)
		{
			throw new ExpiredCursorException("Google Calendar sync token expired.");
		}

		var transactions = new List<Transaction>();

		if (events?.Items != null)
		{
			foreach (var item in events.Items)
			{
				var id = item.Id ?? Guid.NewGuid().ToString();
				var status = item.Status ?? "unknown";
				var summary = item.Summary ?? "";
				
				decimal amount = summary.Contains("VIP") ? 500m : 100m; 

				DateTimeOffset txDate = DateTimeOffset.UtcNow;
				if (item.Start != null)
				{
					if (item.Start.DateTimeDateTimeOffset.HasValue)
						txDate = item.Start.DateTimeDateTimeOffset.Value;
					else if (!string.IsNullOrEmpty(item.Start.Date))
					{
						if (DateTimeOffset.TryParse(item.Start.Date, out var pd))
							txDate = pd;
					}
				}

				transactions.Add(new Transaction
				{
					Source = SourceName,
					SourceTransactionId = id,
					Amount = amount,
					Currency = "USD",
					SourceStatus = status,
					CanonicalStatus = status == "confirmed" ? "completed" : status,
					TransactionDate = txDate,
					CreatedAt = DateTimeOffset.UtcNow,
					UpdatedAt = DateTimeOffset.UtcNow,
					RawPayload = JsonSerializer.Serialize(item)
				});
			}
		}

		return new SyncResult
		{
			Transactions = transactions,
			NextCursor = events?.NextSyncToken
		};
	}
}
