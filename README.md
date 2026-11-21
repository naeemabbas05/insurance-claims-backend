# Chubb APAC Claims Management

Initial .NET 10 modular-monolith backend scaffold. It contains platform plumbing only; no claims or other business workflows are implemented.

## Architecture

- `Chubb.Apac.ClaimsManagement.Api` is the HTTP host and composition root.
- `Chubb.Apac.ClaimsManagement.Application` is reserved for use cases and application contracts.
- `Chubb.Apac.ClaimsManagement.Domain` is reserved for domain models and rules.
- `Chubb.Apac.ClaimsManagement.Infrastructure` contains SQL Server persistence and external adapters.

Dependencies point inward: API composes Application and Infrastructure; Infrastructure depends on Application and Domain. Keep future business modules grouped by module within the relevant layer and register each module from the API composition root. The modules run together in one process and deployable.

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

Set `ConnectionStrings__ClaimsDatabase` in the environment to configure another SQL Server connection string. The API also reads the standard ASP.NET Core configuration sources, including `appsettings.{Environment}.json`, environment variables, and command-line configuration.

## Build

```powershell
dotnet build Chubb.Apac.ClaimsManagement.sln
```
