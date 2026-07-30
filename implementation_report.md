# Step-by-Step Implementation Process

This document outlines the detailed, step-by-step process taken to implement the fault-tolerant sync pipeline and the deterministic metrics service using .NET 9 and Clean Architecture.

---

## Phase 1: Foundation & Domain Modeling
To ensure the system could ingest data from completely different shapes (CRM, Calendar, Payments) without losing fidelity, we started by designing a unified database schema.

1. **Normalized Schema (`Transaction` Entity)**
   - Created a single `Transaction` class containing standard fields: `Source`, `SourceTransactionId`, `Amount`, `Currency`, `SourceStatus`, and `TransactionDate`.
   - Added a `CanonicalStatus` field. This maps diverse statuses (like HubSpot's `closedwon` or Google Calendar's `confirmed`) into a standardized internal vocabulary (e.g., `paid`, `completed`).
   - Added a `RawPayload` (JSONB) column to store the exact JSON returned from the APIs. This guarantees zero data loss if we need to parse additional fields later.

2. **Cursor Tracking (`SyncState` Entity)**
   - Created a database table to keep track of the last cursor/timestamp fetched for each source. This ensures we only fetch *new* data (incremental fetch) rather than downloading the entire dataset every time.

---

## Phase 2: Building the Sync Providers (Problem 1)
We built three independent providers implementing a shared `ISyncProvider` interface.

1. **HubSpot CRM Provider**
   - Integrated with the `/crm/v3/objects/deals` endpoint using `HttpClient`.
   - Used HubSpot's `after` token for pagination/incremental fetches.
   - Mapped the amount, closedate, and dealstage to our normalized schema.

2. **Stripe Payments Provider**
   - Integrated with the `/v1/charges` endpoint using `HttpClient`.
   - Utilized Stripe's `starting_after` cursor to pull only newly created charges.

3. **Google Calendar Provider**
   - Integrated using the official `Google.Apis.Calendar.v3` SDK and a local `credentials.json` OAuth flow.
   - Leveraged the `SyncToken` parameter for incremental event fetches. If the event description contained specific keywords (like "VIP"), it dynamically mapped an amount to the transaction.

---

## Phase 3: Resilience & Orchestration (Problem 1)
The core requirement was that the pipeline "doesn't lie, duplicate data, or crash." We built the `SyncOrchestrator` to handle this.

1. **Fault Isolation**
   - The orchestrator loops through all registered providers. We wrapped the execution of *each* provider in an isolated `try/catch` block. 
   - **Result:** If HubSpot experiences a 500 Internal Server Error, the pipeline simply logs the failure, swallows the exception, and successfully moves on to sync Stripe and Google Calendar.

2. **Idempotent Writes**
   - To prevent duplicate data when fetching overlapping records, we implemented an **Upsert** strategy.
   - When saving to the database, we query by the composite key: `Source` + `SourceTransactionId`. If a record already exists, we update it. If it doesn't, we insert it.

3. **Fallback to Full Backfill**
   - Cursors expire. For example, Google Calendar invalidates `SyncTokens` and throws a `410 Gone` error. 
   - We created a custom `ExpiredCursorException`. If the orchestrator catches this specific exception from a provider, it actively sets the `LastCursor` in the database to `null`. On the very next run, the pipeline natively performs a full backfill.

4. **Background Automation**
   - We wrapped the orchestrator in a .NET `BackgroundService` (`DataSyncHostedService`) configured to run infinitely in the background, waking up every 5 minutes to poll the APIs.

---

## Phase 4: Deterministic Metrics Service (Problem 2)
The requirement was to guarantee that revenue calculations never drift, even if multiple developers work on the codebase.

1. **The Canonical Allow-List**
   - Created a strict `RevenuePolicy` class containing a static array of allowed statuses: `["paid", "completed", "succeeded"]`. 

2. **Compiler-Enforced Encapsulation**
   - We created a Domain entity called `RevenueLedger`. This class is the *only* place in the entire application where the `.Sum(x => x.Amount)` calculation is written.
   - **The Trick:** We refactored the `ITransactionRepository`. Instead of returning a standard `List<Transaction>`, the database query returns a highly-protected `RevenueLedger` object. 
   - **Result:** If a frontend developer wants to build a new graph, they *cannot* query the raw list of transactions to do their own math. They are forced by the compiler to use the `RevenueLedger`, guaranteeing the exact same math is applied globally.

3. **API Endpoints**
   - Built `GET /api/metrics/revenue` which asks the ledger for the `TotalCollectedRevenue`.
   - Built `GET /api/metrics/revenue/breakdown` which uses the exact same ledger to group the data by day or week.

---

## Phase 5: Deployment & Documentation
1. **API Documentation (Scalar UI)**
   - Replaced default Swagger with `Scalar.AspNetCore` for a premium, modern API documentation interface. Configured the root URL (`/`) to automatically redirect to the `/scalar/v1` documentation page.
2. **Containerization**
   - Generated an optimized, multi-stage `Dockerfile` to build the .NET 9 API.
3. **Live Deployment**
   - Connected the GitHub repository directly to Render. Supplied the environment variables (`ConnectionStrings__Supabase`, `HubSpot__PrivateAppToken`, etc.) in the Render dashboard, allowing Docker to compile and host the live API.
