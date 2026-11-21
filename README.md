# Chubb APAC Claims Management

Initial .NET 10 modular-monolith backend with claim submission and tracking endpoints.

## Architecture

- `Chubb.Apac.ClaimsManagement.Api` is the HTTP host and composition root.
- `Chubb.Apac.ClaimsManagement.Application` contains claim submission workflows, validation, and persistence contracts.
- `Chubb.Apac.ClaimsManagement.Domain` contains the claim aggregate, related entities, value objects, market catalog, and domain rules.
- `Chubb.Apac.ClaimsManagement.Infrastructure` contains SQL Server persistence and external adapters.

Dependencies point inward: API composes Application and Infrastructure; Infrastructure depends on Application and Domain. Keep future business modules grouped by module within the relevant layer and register each module from the API composition root. The modules run together in one process and deployable.

## Claims domain

The model supports Motor and Property claims. `MarketCatalog` requires exactly six unique market definitions and can be created from deployment-specific definitions; its defaults are Malaysia (MY/MYR), Indonesia (ID/IDR), Singapore (SG/SGD), Cambodia (KH/KHR), Thailand (TH/THB), and Australia (AU/AUD). Policy currency must match its configured market currency.

Claims require an active policy covering the incident, matching claim and policy types, and at least one claimant. The aggregate enforces UTC timestamps, status transitions, staff market/role permissions, assessment and settlement limits, and claimant-bound information requests. Identifiers are UUIDs; financial amounts use decimal-backed `Money` values.

## Run locally

Requires the .NET 10 SDK. The Development configuration uses SQL Server LocalDB; the API does not connect to the database during startup.
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

## Build

```powershell
dotnet build Chubb.Apac.ClaimsManagement.sln
```

Run the Domain tests with:

```powershell
dotnet test Chubb.Apac.ClaimsManagement.sln
```
