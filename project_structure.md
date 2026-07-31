# RevenueMetrics File & Folder Structure

This repository uses **Clean Architecture** to ensure that business logic, database queries, and API calls are strictly separated. Below is a detailed breakdown of what each folder and file is responsible for.

---

### 1. `RevenueMetrics.Domain` (Core Business Rules)
This project contains the fundamental entities and rules. It has absolutely zero external dependencies (no databases, no HTTP calls).
* **`/Entities/Transaction.cs`**: The single, normalized schema that we map HubSpot, Google Calendar, and Stripe data into. It contains the `RawPayload` JSON column.
* **`/Entities/SyncState.cs`**: The schema used to keep track of the API cursors for incremental fetches.
* **`/Entities/RevenueLedger.cs`**: This is the heart of Problem 2. It takes raw transactions and exposes a strict `.TotalCollectedRevenue` property. This forces all calculations in the app to use one canonical formula.
* **`/Policies/RevenuePolicy.cs`**: Holds the strict "allow-list" of statuses that count as collected revenue (`paid`, `completed`, `succeeded`).

### 2. `RevenueMetrics.Application` (Use Cases & Interfaces)
This project defines *what* the application does, but not *how* it does it.
* **`/Interfaces/ISyncProvider.cs`**: The blueprint that every API source (HubSpot, Stripe, etc.) must follow to be part of the sync pipeline.
* **`/Interfaces/ITransactionRepository.cs`**: The contract for talking to the database. Notice that it forces the database to return a `RevenueLedger` instead of a raw list, structurally preventing developers from inventing their own math.
* **`/Exceptions/ExpiredCursorException.cs`**: A custom exception thrown when an API (like Google Calendar throwing a 410) rejects an old cursor, signaling the pipeline to fall back to a full backfill.
* **`/Models/`**: Contains Data Transfer Objects (DTOs) like `SyncResult`, and the JSON shapes for the API responses (`RevenueBreakdownResponse`).

### 3. `RevenueMetrics.Infrastructure` (Databases & External APIs)
This project contains the actual messy implementation details for talking to the outside world.
* **`/Persistence/AppDbContext.cs`**: The Entity Framework Core setup that connects to your Supabase PostgreSQL database.
* **`/Migrations/`**: Auto-generated files that track changes to your database schema.
* **`/Repositories/TransactionRepository.cs`**: The actual SQL/LINQ queries that pull transactions from Supabase.
* **`/Services/SyncProviders/HubSpotSyncProvider.cs`**: Uses `HttpClient` to hit HubSpot's CRM endpoints and map Deals into our `Transaction` schema.
* **`/Services/SyncProviders/StripeSyncProvider.cs`**: Uses `HttpClient` to hit Stripe's Payment endpoints using `starting_after` cursors.
* **`/Services/SyncProviders/GoogleCalendarSyncProvider.cs`**: Uses the official Google SDK and OAuth (`credentials.json`) to fetch events.
* **`/Services/SyncOrchestrator.cs`**: The "brain" of Problem 1. It loops through the providers, wraps them in `try/catch` blocks for fault isolation, handles the `ExpiredCursorException` fallback logic, and idempotently upserts the data into the database.

### 4. `RevenueMetrics.API` (The Presentation Layer)
This is the entry point that runs the actual application, exposes endpoints, and runs background jobs.
* **`/Controllers/MetricsController.cs`**: Exposes the actual HTTP endpoints (`GET /api/metrics/revenue` and `GET /api/metrics/revenue/breakdown`).
* **`/HostedServices/DataSyncHostedService.cs`**: A native .NET BackgroundService that automatically wakes up every 5 minutes and tells the `SyncOrchestrator` to fetch new data.
* **`Program.cs`**: The setup file. This is where we inject Supabase, register our HTTP clients, and configure the Scalar API UI.
* **`appsettings.json`**: Holds your configuration variables (which you map to Render Environment Variables) like the Supabase connection string and API keys.

### 5. Root Level Files (Deployment)
* **`Dockerfile`**: Instructions for Render (or Docker) on how to compile the .NET 9 code and host it on a Linux server.
* **`README.md`**: The main documentation.
* **`implementation_report.md`**: The detailed breakdown of how we solved the specific requirements of the assignment.
* **`account_setup_guide.md`**: The guide summarizing how to provision the API keys.
* **`credentials.json` & `token.json`**: *(Ignored in `.gitignore`)* Your private Google Calendar OAuth files for local testing.
