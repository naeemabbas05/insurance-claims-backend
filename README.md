# Chubb APAC Claims Management Backend

.NET 10 modular-monolith backend for Chubb APAC motor and property claims. The solution includes claim submission, lifecycle and staff operations, management reporting, SQL Server persistence, JWT authorization, and Kafka outbox publishing.

## Architecture

- `Chubb.Apac.ClaimsManagement.Api` is the HTTP host and composition root.
- `Chubb.Apac.ClaimsManagement.Application` contains claim submission workflows, validation, and persistence contracts.
- `Chubb.Apac.ClaimsManagement.Domain` contains the claim aggregate, related entities, value objects, market catalog, and domain rules.
- `Chubb.Apac.ClaimsManagement.Infrastructure` contains SQL Server persistence and external adapters.

Dependencies point inward: API composes Application and Infrastructure; Infrastructure depends on Application and Domain. Keep future business modules grouped by module within the relevant layer and register each module from the API composition root. The modules run together in one process and deployable.

### Entity relationships

- A `MarketDefinition` has many `Policy` and `StaffUser` relationships. Staff-to-market membership is many-to-many.
- A `Policy` belongs to one market and can have many `Claim` records.
- `Claim` is the aggregate root. It owns its claimants, documents, assignments, assessments, status history, information requests, settlements, and audit entries.
- Assignments link claims to staff; assessments and status changes may reference staff actors. Information requests link a claim, claimant, and requesting staff member. Settlements link a claim and payee claimant, and record staff actors for proposal, approval, and payment.
- Claim submission idempotency records point to the claim created by the original request. Outbox rows contain claim identifiers and event payloads for delivery.

The aggregate enforces claim and financial rules; EF Core configurations define the relational keys, indexes, concurrency tokens, and delete behavior.

### REST and Kafka

REST is used for synchronous claimant, staff, and management workflows: callers need immediate validation, authorization, and a response describing the committed result. SQL Server is the system of record.

Kafka is used for asynchronous downstream notifications, not for request/response APIs or as the claims database. Versioned event records are written to the SQL transactional outbox with the claim change, then published in the background. This avoids making claim commits depend on broker availability. Delivery is at-least-once, so consumers must deduplicate by `EventId`.

## Claims domain

The model supports Motor and Property claims. `MarketCatalog` requires exactly six unique market definitions and can be created from deployment-specific definitions; its defaults are Malaysia (MY/MYR), Indonesia (ID/IDR), Singapore (SG/SGD), Cambodia (KH/KHR), Thailand (TH/THB), and Australia (AU/AUD). Policy currency must match its configured market currency.

Claims require an active policy covering the incident, matching claim and policy types, and at least one claimant. The aggregate enforces UTC timestamps, status transitions, staff market/role permissions, assessment and settlement limits, and claimant-bound information requests. Identifiers are UUIDs; financial amounts use decimal-backed `Money` values.

## Run locally

Requires the .NET 10 SDK. The Development configuration uses SQL Server LocalDB. The API does not apply migrations automatically; apply them before using database-backed endpoints. The background outbox publisher starts polling SQL Server after host startup.
In Development, JWT authority and audience settings are optional so the API, health checks, and Swagger can run without an identity provider; authenticated endpoints remain unavailable until JWT settings are supplied. Outside Development, both settings are required at startup. See [Authentication and authorization](#authentication-and-authorization) to configure them.

```powershell
dotnet run --project src\Chubb.Apac.ClaimsManagement.Api
```

- Versioned status endpoint: `http://localhost:5187/api/v1/status`
- Liveness: `http://localhost:5187/health/live`
- Readiness: `http://localhost:5187/health/ready`
- Swagger UI (Development only): `http://localhost:5187/swagger`

The readiness check currently reports API process health only; SQL connectivity is not checked.

## Claim submission

Submit a Motor or Property claim with a unique `Idempotency-Key` header. The API validates the configured market, policy coverage, claimant contact information, UTC incident time, estimated loss, and policy currency. A successful request returns `201 Created` with a claim reference and tracking URL. Repeating the same key and payload returns the original claim; using the key with a different payload returns `409 Conflict`.

```http
POST /api/v1/claims
Authorization: Bearer <claimant-jwt>
Idempotency-Key: submission-key-123
Content-Type: application/json

{
  "marketCode": "MY",
  "policyNumber": "APAC-MY-MOTOR-001",
  "claimType": "motor",
  "incidentAtUtc": "2026-03-10T08:30:00Z",
  "incidentDescription": "Vehicle damage following a road accident.",
  "claimant": {
    "name": "Avery Tan",
    "email": "avery@example.com",
    "phone": "+60123456789"
  },
  "estimatedLossAmount": 1200.00,
  "currencyCode": "MYR"
}
```

Track a submitted claim using `GET /api/v1/claims/{claimReference}`.

Staff lifecycle calls use a `ClaimsOfficer` or `Manager` JWT. For example:

```http
POST /api/v1/claims/{claimReference}/assessments
Authorization: Bearer <staff-jwt>
Content-Type: application/json

{
  "assessedDamageAmount": 1000.00,
  "recommendedSettlementAmount": 800.00,
  "currencyCode": "MYR",
  "findings": "Repair estimate reviewed."
}
```

## Claim lifecycle

Lifecycle operations enforce role and market rules in the Domain aggregate, and every transition or material action is written to `ClaimAuditHistory`. Status transitions are also retained in `ClaimStatusHistory`.

- `GET /api/v1/claims/{claimReference}/history` returns the status timeline.
- `GET /api/v1/claims/{claimReference}/audit` returns audit details to staff assigned to the claim's market.
- `POST /api/v1/claims/{claimReference}/assessments` records assessment findings and recommended settlement.
- `POST /api/v1/claims/{claimReference}/approval` requires a positive approved amount no greater than the latest recommendation.
- `POST /api/v1/claims/{claimReference}/rejection` requires a reason. Settled claims cannot be rejected.
- `POST /api/v1/claims/{claimReference}/information-requests` requests claimant information; `POST .../information-requests/{requestId}/responses` records the claimant response. `DELETE .../information-requests/{requestId}` cancels an open request.
- `POST /api/v1/claims/{claimReference}/settlements` proposes a settlement. The total of active settlements cannot exceed the approved amount. Settlement approval, payment, and cancellation use `POST .../settlements/{settlementId}/approval`, `POST .../payment`, and `DELETE .../settlements/{settlementId}`.
- `POST /api/v1/claims/{claimReference}/workflow-status` supports handler-driven reopening to `underReview` and closing.

Lifecycle writes require a JWT with the `ClaimsOfficer` or `Manager` role and a `staff_user_id` claim (a UUID `sub` is also accepted). Claimant information responses require the `Claimant` role and a verified owner identity. Staff access is checked against the staff user's active market memberships and operational role in the database.

## Claims staff operations

Staff APIs require the `ClaimsOfficer` or `Manager` JWT role and a staff identity (`staff_user_id` or UUID `sub`). The API role must agree with the staff user's operational role, and the actor's active markets are checked. Queue and officer claim lists are paged with `page` (1-based) and `pageSize` (1-100).

- `GET /api/v1/staff/claims/incoming?marketCode=MY&page=1&pageSize=20` lists submitted claims.
- `GET /api/v1/staff/claims/unassigned?marketCode=MY&page=1&pageSize=20` lists non-closed, non-settled claims with no active assignments.
- `GET /api/v1/staff/officers/{officerId}/claims?marketCode=MY&page=1&pageSize=20` lists an officer's active assignments.
- `GET /api/v1/staff/officers/{officerId}/workload?marketCode=MY` returns active claim and role counts.
- `POST /api/v1/staff/claims/{claimReference}/assignments` assigns an officer and role; `POST .../assignments/{assignmentId}/reassign` reassigns that role to another officer.
- `GET /api/v1/staff/claims/{claimReference}/assignments` returns assignment history, including prior officers and unassignment timestamps.

Only one active assignment per role is permitted on a claim. Assignment changes update the claim's concurrency version; stale concurrent changes return `409 Conflict`. Reassignment closes the prior assignment record and creates a new one so history is preserved.

Example assignment request:

```http
POST /api/v1/staff/claims/{claimReference}/assignments
Authorization: Bearer <staff-jwt>
Content-Type: application/json

{
  "officerId": "00000000-0000-0000-0000-000000000001",
  "role": "adjuster"
}
```

## Management reporting

`GET /api/v1/management/reports/claims` returns claim totals and breakdowns by status, market, and type, active workload by officer, and financial totals. Optional filters are `marketCode`, `fromDate`, and `toDate`:

```http
GET /api/v1/management/reports/claims?marketCode=MY&fromDate=2026-01-01&toDate=2026-12-31
```

Dates are UTC calendar dates applied inclusively to the claim's `ReportedAtUtc`. If `marketCode` is omitted, the report covers all markets assigned to the requesting staff member. Access is limited to active Approver, FinanceOfficer, and Administrator staff; all results remain restricted to their assigned markets. Unassigned claims and officer workload count only non-closed, non-settled claims with active assignments.

Financial totals are grouped by currency and returned as separate lists; the API does not convert or combine currencies. Outstanding estimated exposure sums estimates for claims not declined, settled, or closed. Assessed amount uses each claim's latest assessment. Approved amount sums claim-level approvals. Approved but unpaid liability sums approved settlement transactions that have not been paid; settled amount sums paid settlement transactions. All these measures use the same reported-date and market-filtered claim cohort.

The infrastructure layer reads `ConnectionStrings__ClaimsDatabase` from standard ASP.NET Core configuration. Set the environment variable to override the Development LocalDB value:

```powershell
$env:ConnectionStrings__ClaimsDatabase = "Server=.\SQLEXPRESS;Database=ChubbClaimsManagement;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True"
```

## Authentication and authorization

The API validates JWT bearer tokens against an external OpenID Connect authority. Configure these environment variables before starting the API:

```powershell
$env:Jwt__Authority = "https://identity.example.com"
$env:Jwt__Audience = "chubb-apac-claims-api"
$env:Jwt__RoleClaimType = "role"
```

`Jwt:Authority` must use HTTPS outside Development; no issuer secrets or signing keys are stored in this repository. Tokens must have one of the exact role values `Claimant`, `ClaimsOfficer`, or `Manager`. The role claim name is configurable (for example, `roles`). Identity claims are not URI-mapped.

- Claimant access uses a trusted `claimant_id` UUID, or a matching `email` only when `email_verified` is `true`. Claim submission also requires that verified email to match the submitted claimant email.
- Staff identity is taken from `staff_user_id`, UUID `sub`, or the standard `NameIdentifier` claim. The matching active `StaffUser` record must have the operational role appropriate to the token role and be assigned to the requested claim market.
- `ClaimsOfficer` maps to operational handler, adjuster, assessor, approver, or finance roles; `Manager` maps to administrator, approver, or finance roles. Domain operation-specific permissions still apply.
- Claimant routes are limited to the authenticated claimant's claims. Staff claim reads and changes are restricted by token role and the persisted staff market membership.

## Kafka events and transactional outbox

Claim submission, assignment/reassignment, status changes, approval, rejection, and settlement enqueue versioned event contracts in the same SQL transaction as the claim update. The background publisher sends outbox records to Kafka; a broker outage does not undo a committed claim change. Configure the broker and topic with:

```powershell
$env:Kafka__BootstrapServers = "localhost:9092"
$env:Kafka__ClaimEventsTopic = "claims.events"
```

The versioned contracts are `ClaimSubmittedV1`, `ClaimAssignedV1`, `ClaimStatusChangedV1`, `ClaimApprovedV1`, `ClaimRejectedV1`, and `ClaimSettledV1`. Delivery is at-least-once: consumers should deduplicate using the `EventId` in the event envelope (also provided in the Kafka `event-id` header). Messages are keyed by claim reference. If Kafka is not configured or available, events remain persisted and publishing is retried with backoff.

## SQL Server persistence

EF Core migrations are stored with Infrastructure. Restore the repository-pinned EF tool and apply migrations with a configured SQL Server connection:

```powershell
dotnet tool restore
dotnet ef database update --project src\Chubb.Apac.ClaimsManagement.Infrastructure --startup-project src\Chubb.Apac.ClaimsManagement.Api --context ClaimsDbContext
```

The migrations create the claims and transactional outbox schemas, rowversion concurrency columns, indexes and foreign keys, and seed six APAC markets, sample Motor and Property policies, and sample staff users. Claim submission uses a repository to atomically persist claims and idempotency records.

## Build and tests

```powershell
dotnet restore Chubb.Apac.ClaimsManagement.sln
dotnet tool restore
dotnet build Chubb.Apac.ClaimsManagement.sln
dotnet test Chubb.Apac.ClaimsManagement.sln
```

The xUnit projects cover domain rules, submission/idempotency, lifecycle transitions, approval/rejection/settlement, staff assignments, authorization, and reporting. An opt-in SQL Server integration test runs a full submission-to-settlement journey against the actual migrations and repository queries, including financial exposure and outbox persistence. It creates and drops a uniquely named test database:

```powershell
$env:CHUBB_CLAIMS_SQLSERVER_TEST_CONNECTION_STRING = "Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True"
dotnet test tests\Chubb.Apac.ClaimsManagement.Infrastructure.IntegrationTests
```

The integration test is reported as skipped when `CHUBB_CLAIMS_SQLSERVER_TEST_CONNECTION_STRING` is unset. Its SQL Server login must be allowed to create and drop databases. Do not point the test at a production SQL Server.

## Assumptions

- The deployment supplies exactly the six configured APAC market definitions. Defaults are MY/MYR, ID/IDR, SG/SGD, KH/KHR, TH/THB, and AU/AUD.
- The identity provider is external and issues trusted JWTs with the documented roles and identity claims. Development can start without JWT settings for health/Swagger use, but protected routes still require a valid token.
- Claim and incident timestamps are UTC. Management date filters are inclusive UTC calendar dates over `ReportedAtUtc`.
- Financial report values remain separated by currency. No FX rate source or conversion policy is implemented.
- Sample policies and staff are migration seed data for development/testing, not a production onboarding process.
- Claim event consumers accept at-least-once delivery and deduplicate by event ID.

## Limitations and future improvements

- Configure and validate a production identity provider, role provisioning, and staff lifecycle; this repository does not issue JWTs or manage IdP users.
- Readiness currently checks API process health only, not SQL Server or Kafka connectivity.
- Kafka publishing is implemented, but inbound consumers, schema registry integration, dead-letter operations, and event retention are not.
- Documents currently have domain metadata only; binary upload, malware scanning, retention, and secure object storage need implementation.
- Reporting intentionally does not convert currencies. A governed FX source and explicit reporting-date conversion policy would be required before adding consolidated totals.
- Production hardening should add distributed tracing, operational metrics/alerts for outbox age and retries, database backup/retention policies, and CI execution of SQL Server integration tests.
- Further market-specific regulatory workflows, localization, and configurable claim reference rules can be added as requirements are confirmed.

## Development journal

See [DEVELOPMENT_JOURNAL.md](DEVELOPMENT_JOURNAL.md) for the development history and final assessment notes.
