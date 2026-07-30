# RevenueMetrics API & Sync Pipeline

This repository contains the solution for the Full-Stack Backend Assignment. It features a fault-tolerant, idempotent data sync pipeline and a deterministic metrics calculation service built using .NET 9 Clean Architecture.

## Features

### Problem 1: Sync Pipeline
- **Sources**: Ingests data from HubSpot CRM, Google Calendar, and Stripe.
- **Normalization**: Maps diverse API payloads into a single, unified `Transaction` schema.
- **Incremental Fetches**: Tracks cursors for each source via a `SyncState` database table.
- **Resilience**: 
  - If a source throws an error (e.g., HTTP 500), the `SyncOrchestrator` logs it, isolates the fault, and proceeds to the next source without wedging the job.
  - If an API rejects a cursor (e.g. Google Calendar 410 Gone), the pipeline catches the specific exception, clears the cursor, and safely falls back to a full backfill on the next run.
- **Idempotency**: Upserts records based on a composite key (`Source` + `SourceTransactionId`) to guarantee zero duplicate rows.

### Problem 2: Metrics Service
- **Deterministic Number**: The `RevenueLedger` domain entity acts as the single source of truth, computing revenue using a strict allow-list of canonical statuses (`paid`, `completed`, `succeeded`).
- **Compiler-Enforced Consistency**: To guarantee that no one accidentally adds a "second way" of calculating revenue, the Repository pattern was refactored to return a strongly typed `RevenueLedger` rather than a standard `List<Transaction>`. This structural constraint makes it impossible for developers to query transactions by date without passing through the canonical calculation.
- **Two Views**: Exposes both a summary endpoint (`/api/metrics/revenue`) and a breakdown endpoint (`/api/metrics/revenue/breakdown?interval=week`). Both views utilize the exact same ledger, guaranteeing they never drift.

## Running Locally

1. Create a free Supabase PostgreSQL project and grab the connection string.
2. Obtain a HubSpot Private App token, Stripe secret key, and Google Calendar `credentials.json`.
3. Add your keys to `appsettings.json` and place `credentials.json` at the root of the API directory.
4. Run EF Core migrations to build the schema:
   ```bash
   dotnet ef database update --project RevenueMetrics.Infrastructure --startup-project RevenueMetrics.API
   ```
5. Run the API:
   ```bash
   dotnet run --project RevenueMetrics.API
   ```
The background service (`DataSyncHostedService`) will automatically wake up and begin pulling data every 5 minutes.

## Deployment (Render)
A `Dockerfile` is included at the root of the project. To deploy on Render:
1. Create a new "Web Service" connected to this GitHub repo.
2. Select "Docker" as the runtime environment.
3. Add your environment variables in the Render dashboard:
   - `ConnectionStrings__Supabase`
   - `HubSpot__PrivateAppToken`
   - `Stripe__SecretKey`

## Sources & References
- [HubSpot CRM API Documentation](https://developers.hubspot.com/docs/api/crm/deals)
- [Google Calendar .NET SDK](https://developers.google.com/calendar/api/quickstart/dotnet)
- [Stripe API Documentation](https://stripe.com/docs/api)
- [EF Core PostgreSQL Provider (Npgsql)](https://www.npgsql.org/efcore/)
