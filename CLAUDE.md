# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Scope

Ecommerce "TheOffice". Only the catalog backend exists today (`src/`): products, categories, customers REST API. Frontend, cart/orders, backoffice, and auth are not built yet — don't assume they exist.

## Commands

Run from `src/`.

```bash
dotnet restore
dotnet build TheOffice.sln
dotnet run --project Presentation/TheOffice.Api
```

API on `http://localhost:5226`, interactive docs (Scalar) on `http://localhost:5226/scalar`. In `Development`, `Program.cs` applies EF Core migrations and seeds data automatically on startup — no manual DB setup needed. `theoffice.db` is local/gitignored.

Migrations (run from `src/`):

```bash
dotnet ef migrations add <Name> -p Infrastructure/TheOffice.Persistence -s Presentation/TheOffice.Api
dotnet ef database update -p Infrastructure/TheOffice.Persistence -s Presentation/TheOffice.Api
```

Tests (run from repo root):

```bash
dotnet test src/TheOffice.sln
```

No lint/formatter config exists in this repo yet.

## Tests

`tests/` (sibling of `src/`, outside the solution's physical tree but registered in `TheOffice.sln` under a `Tests` solution folder). One project per project under test: `tests/TheOffice.Application.Tests/`, with files mirroring the source layout (`Services/ProductServiceTests.cs`).

- xUnit v3 (`xunit.v3.mtp-v2`) + NSubstitute. Assertions with xUnit's native `Assert` — response DTOs are `record`s, so `Assert.Equal` compares whole objects structurally.
- `global.json` at repo root opts `dotnet test` into Microsoft.Testing.Platform — required by xunit.v3. Don't remove it.
- Test names are Spanish, `Metodo_Escenario_ResultadoEsperado`.
- Only `ProductService` is covered so far. `ProductController` is not unit-testable as-is: it depends on the concrete `ProductService` whose methods aren't `virtual`. Covering it needs either an extracted `IProductService` or integration tests via `WebApplicationFactory` (which would need a `public partial class Program` in `Program.cs`, currently absent).
- Repository behavior (the `IsActive` filter, slug trimming, `LIKE` search, ordering) lives in `ProductRepository` and is only reachable via integration tests against SQLite in-memory — note `Price` uses `HasConversion<double>()`.

## Architecture

Clean Architecture, 5 projects, dependencies point inward:

```
Presentation (TheOffice.Api)      controllers, CORS, API versioning, OpenAPI
   -> Application (TheOffice.Application)   services, DTOs, interfaces, mappers
Infrastructure/TheOffice.Persistence  -> Application   EF Core, repositories, migrations, seeders
Infrastructure/TheOffice.Adapters     -> Application   external-service adapters (notifications)
Application -> Domain (TheOffice.Domain)   entities, enums, Result pattern, constants
```

- **Domain** has no dependencies on other layers.
- **Application** defines the interfaces infra implements (`IProductRepository`, `ICategoryRepository`, `ICustomerRepository` under `Interfaces/Persistence`; `INotificationAdapter` under `Interfaces/Adapters`), plus the DTOs, service classes, and mappers between domain entities and DTOs.
- **Persistence** implements repositories, has its own EF Core `Models/` (separate from `Domain/Entities`) with its own mappers to translate between them, plus `TheOfficeDbContext`, `Migrations/`, `Seeders/`.
- **Adapters** implements things like `INotificationAdapter` (currently a console-based stub).
- **Api** (Presentation) is thin: controllers call a single `*Service` from Application and translate `Result`/`Result<T>` into HTTP responses. No business logic here.
- Each project layer (`Application`, `Persistence`, `Adapters`) exposes its DI wiring via an `AddXxx(...)` extension method in a `DependencyInjection.cs` at its root, composed together in `Program.cs`.

### Key conventions

- **Result pattern** (`TheOffice.Domain.Common.Result` / `Result<T>`): used instead of exceptions for expected failures (validation, not-found business rules, data-access failures). Controllers check `IsSuccess`/`Error` and map to `BadRequest`/`Ok`. Don't throw for control flow in services/repositories — return `Result.Failure(...)`.
- **Public IDs**: every externally exposed entity has both a private `Id` (Guid, internal) and a `PublicId` (string, e.g. `PRD-001`, `CAT-001`, `CUS-001`). All API routes/requests use `PublicId`, never the internal `Id`.
- **Domain entities != persistence models**: `Domain/Entities` are plain business objects; `Persistence/Models` are the EF Core-mapped shapes (see `Persistence/Models/BaseModel.cs`). Each has its own mapper (`Persistence/Mappers/*` translates Model<->Domain; `Application/Mappers/*` translates Domain<->DTO). Don't reuse one model where the other is expected.
- **DTOs live in Application**, shared across layers rather than duplicated per-consumer (see README "Decisiones de implementación" #1 for rationale) — don't add a separate DTO layer for the Web API contracts.
- Async method names do **not** have an `Async` suffix (e.g. `GetPaged`, not `GetPagedAsync`) — this is deliberate, keep it consistent.
- `Price` (decimal) is stored with `HasConversion<double>()` in `TheOfficeDbContext` because SQLite can't sort/compare `decimal` (stored as TEXT). This is a SQLite-only workaround, meant to be removed when migrating to SQL Server (see README) — don't apply the same pattern to other decimal fields without cause.
- Indentation is 2 spaces (see any existing `.cs` file).
- API routes are versioned: `[Route("api/v{version:apiVersion}/...")]`, currently only `1.0`.

### Data model

`Category` 1—N `Product` (FK `CategoryId`, `DeleteBehavior.Restrict`). `Customer` is standalone (has a `Source` enum: Website/Email/Phone/SocialMedia), meant as the future base for cart/orders.
