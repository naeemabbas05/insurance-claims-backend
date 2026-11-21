# Chubb APAC Claims Management

Initial .NET 10 modular-monolith backend. The Domain layer now contains the claims model and its business rules; no claim-management HTTP endpoints or application workflows are implemented yet.

## Architecture

- `Chubb.Apac.ClaimsManagement.Api` is the HTTP host and composition root.
- `Chubb.Apac.ClaimsManagement.Application` is reserved for use cases and application contracts.
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

The initial readiness check reports API process health only; SQL connectivity is not checked because no database-backed module is implemented yet.

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

The initial migration creates the claims schema, rowversion concurrency columns, indexes and foreign keys, and seeds six APAC markets, sample Motor and Property policies, and sample staff users. No repositories are added yet; `ClaimsDbContext` is the persistence boundary until use cases require repository abstractions.

## Build

```powershell
dotnet build Chubb.Apac.ClaimsManagement.sln
```

Run the Domain tests with:

```powershell
dotnet test Chubb.Apac.ClaimsManagement.sln
```
