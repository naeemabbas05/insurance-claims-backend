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

Lifecycle writes require an authenticated principal carrying `staff_user_id` (or `NameIdentifier`) for staff actions; claimant information responses require `claimant_id`. The starter host does not configure an authentication provider, so wire one before enabling these operations in a deployed environment.

The infrastructure layer reads `ConnectionStrings__ClaimsDatabase` from standard ASP.NET Core configuration. Set the environment variable to override the Development LocalDB value:

```powershell
$env:ConnectionStrings__ClaimsDatabase = "Server=.\SQLEXPRESS;Database=ChubbClaimsManagement;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True"
```

## SQL Server persistence

EF Core migrations are stored with Infrastructure. Restore the repository-pinned EF tool and apply migrations with a configured SQL Server connection:

```powershell
dotnet tool restore
dotnet ef database update --project src\Chubb.Apac.ClaimsManagement.Infrastructure --startup-project src\Chubb.Apac.ClaimsManagement.Api --context ClaimsDbContext
```

The migrations create the claims schema, rowversion concurrency columns, indexes and foreign keys, and seed six APAC markets, sample Motor and Property policies, and sample staff users. Claim submission uses a repository to atomically persist claims and idempotency records.

## Build

```powershell
dotnet build Chubb.Apac.ClaimsManagement.sln
```

Run the Domain tests with:

```powershell
dotnet test Chubb.Apac.ClaimsManagement.sln
```
