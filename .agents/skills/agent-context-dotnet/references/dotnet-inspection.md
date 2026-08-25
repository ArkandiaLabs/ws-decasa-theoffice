# .NET inspection checklist

The discovery work behind `docs/dotnet.md`. Each section says **what to read**, **what to
extract**, and **how to record it**. Read real files (Glob/Grep/Read). Where a fact isn't
readable, leave a `<!-- TODO -->` — never guess.

The signal set here is grounded in what static analysis of real .NET solutions surfaces
(multi-project solutions, per-project TFMs, NuGet graphs with vuln scanning, EF Core `DbContext`
hotspots, absent analyzers/arch-linting, Azure Pipelines, MAUI `Platforms/` splits, secrets in
`appsettings`), updated for the .NET 10 / C# 14 era. Treat the list as the floor, not the ceiling.

**Every section is conditional.** Inspect only what the repo actually signals, and carry that
through to the document: a single-project console app should produce a `docs/dotnet.md` with the
Aspire, UI, and MAUI sections **deleted**, not filled with TODOs. A short accurate doc beats a
long hedged one.

---

## 1. Solution & projects

- **Read:** `*.sln` (text) and `*.slnx` (newer XML format — handle both). Then every `*.csproj` /
  `*.fsproj` / `*.vbproj`.
- **Extract:**
  - Solution file name(s) and the project list.
  - Classify each project. Heuristics:
    - `Microsoft.NET.Sdk.Web` + `Controllers/` or `Program.cs` mapping endpoints → **Web API / MVC**.
    - `.cshtml` present → **Razor Pages / MVC**.
    - `Microsoft.NET.Sdk.Razor` + `.razor` → **Blazor / Razor Class Library**.
    - `<UseMaui>true</UseMaui>` or `net*-android`/`net*-ios` TFMs → **MAUI app**.
    - `Microsoft.NET.Sdk.Worker` / `IHostedService` → **Worker / background service**.
    - `Aspire.Hosting.AppHost` / name ends in `.AppHost` → **Aspire AppHost** (see §4).
    - name ends in `.ServiceDefaults` → **Aspire service defaults** (see §4).
    - test packages (xUnit/NUnit/MSTest) → **Test project**.
    - otherwise → **Class Library / Console**.
  - **Project-reference graph:** parse `<ProjectReference>` to see who depends on whom. This reveals
    layering (e.g. `Api → Core → Infrastructure`) and proxy/shared libraries.
- **Record:** a project table (name · type · TFM · one-line purpose) and a small Mermaid graph of
  the project references.

## 2. Target frameworks & language

- **Read:** each `*.csproj`; `Directory.Build.props` / `Directory.Build.targets` (shared settings);
  `global.json` (pinned SDK).
- **Extract:** `<TargetFramework>` / `<TargetFrameworks>` (multi-targeting, incl. `net10.0-android`,
  `net10.0-ios`), `<LangVersion>`, `<Nullable>` (`enable`/`disable`), `<ImplicitUsings>`,
  `<TreatWarningsAsErrors>`, `<UserSecretsId>`.
- **Record:** the TFM(s), the C# language posture (nullable on/off, implicit usings), and any
  pinned SDK. Flag inconsistent TFMs across projects (an ambiguity for claim validation).

## 3. Package management & dependencies

- **Read:** `<PackageReference>` across all projects; `Directory.Packages.props`;
  `packages.lock.json`; `nuget.config`; `.config/dotnet-tools.json`.
- **Extract:**
  - **Central package management (CPM).** Does `Directory.Packages.props` exist, and is
    `<ManagePackageVersionsCentrally>` true? If so, versions live there as `<PackageVersion>` and a
    `<PackageReference>` in a `.csproj` **must not carry a `Version` attribute**. Record this as an
    *editing rule* for AGENTS.md, not just a fact — it is one of the things an agent most reliably
    gets wrong.
  - **Feeds.** `nuget.config` sources — private or internal feeds an agent needs to know exist.
  - **Audit posture.** `<NuGetAudit>`, `<NuGetAuditMode>`, `<NuGetAuditLevel>`, and whether a
    lockfile is committed. In .NET 10, `RestoreEnablePackagePruning` is on by default for
    `net10.0`+ and prunes framework-provided package references (which also shrinks the generated
    `.deps.json`); note it only if the repo sets it explicitly.
  - **Local tool manifest.** `.config/dotnet-tools.json` — if `dotnet-ef`, `dotnet-format`,
    `csharpier` etc. are pinned there, the commands are `dotnet tool restore` then
    `dotnet <tool>`, not the global equivalents. This changes §8.
  - **Load-bearing packages**, grouped — web framework (ASP.NET Core), ORM
    (`Microsoft.EntityFrameworkCore.*`, `Dapper`), mediator (`MediatR`), mapping (`AutoMapper`),
    logging (`Serilog`, `NLog`), auth (`Microsoft.Identity*`, Duende `IdentityServer`), messaging,
    resilience (`Polly`, `Microsoft.Extensions.Http.Resilience`). Note versions.
- **Record:** a short highlights list (not the full dump), the CPM rule if it applies, and the feed
  situation. Note version freshness or known-vuln risk **only** if you have a concrete signal
  (e.g. a lockfile audit) — do not invent CVEs.

## 4. Aspire orchestration (skip if absent)

Aspire changes what "run the app" means, so when it is present this section feeds the Commands
block in AGENTS.md directly.

- **Read:** any `*.AppHost` project — `AppHost.cs` (or a file-based `apphost.cs`) and its
  `DistributedApplication.CreateBuilder` call; `Aspire.Hosting.*` packages; any `*.ServiceDefaults`
  project and its `Extensions.cs` / `AddServiceDefaults()`.
- **Extract:** the resources the AppHost wires up (databases, caches, queues, projects) and how
  services reference each other; what ServiceDefaults centralizes — typically OpenTelemetry, health
  check endpoints, service discovery, and `HttpClient` resilience.
- **Record:**
  - the orchestration topology (which projects the AppHost composes, plus backing services);
  - that the local entry point is **`aspire run` / `dotnet run --project <…>.AppHost`**, not each
    service individually;
  - the convention that ServiceDefaults holds shared cross-cutting wiring only — an agent should
    not park models or business logic there;
  - that cross-cutting concerns in §11 are probably already supplied by ServiceDefaults, so an
    agent should not re-register them per service.

## 5. Composition root / DI

- **Read:** `Program.cs` (minimal hosting) and/or `Startup.cs`; any Autofac `Module` classes;
  `*Extensions.cs` files with `AddXxx(this IServiceCollection)` patterns.
- **Extract:** where services are registered, the container (built-in vs Autofac/others), and
  notable lifetimes (`AddSingleton`/`AddScoped`/`AddTransient`) or conventions (assembly scanning,
  options pattern, hosted services).
- **Record:** where the composition root lives and any DI convention an agent must follow.

## 6. Data access (EF Core)

- **Read:** classes deriving from `DbContext`; `OnModelCreating`; `Migrations/` folders;
  `UseSqlServer` / `UseNpgsql` / `UseSqlite` / `UseCosmos` calls; connection-string config.
- **Extract:** the provider, the `DbContext` location(s), how the model is configured (data
  annotations vs Fluent API vs `IEntityTypeConfiguration<>`), where migrations live and how they
  are applied (`dotnet ef database update`, `context.Database.Migrate()` on startup, or in CI).
- **EF Core 10 specifics worth checking:**
  - **Named query filters** — EF Core 10 supports multiple query filters per entity type, each
    named, with selective disabling. If the repo uses them, an agent must know a filter can be
    turned off by name. This is exactly the tenant-scoping / soft-delete invariant that belongs in
    AGENTS.md's "Non-obvious rules".
  - Compiled models (`dotnet ef dbcontext optimize`) and migration bundles, if used.
  - Whether `dotnet ef` is a global tool or pinned in `.config/dotnet-tools.json` (see §3).
- **Record:** provider + `DbContext` location + migration workflow. **Flag a very large
  `DbContext`** (thousands of LOC) as a hotspot. Cross-link `docs/data-model.md` for the schema.

## 7. Configuration & secrets

- **Read:** `appsettings.json` and `appsettings.{Environment}.json`; `launchSettings.json`;
  `<UserSecretsId>`; Key Vault / environment-variable usage; `.env` files.
- **Extract:** the configuration layering (base → env → user-secrets/env vars → Key Vault), and
  what each environment overrides.
- **Record:** the secrets strategy. **Flag any secret that appears committed** (connection strings,
  API keys, storage keys in `appsettings`) — recommend moving to user-secrets / Key Vault and an
  `appsettings.example.json`. Do not reproduce the secret value in the docs.

## 8. Build, run, test

- **Read:** `*.sln`, test projects, `global.json`, CI files, any `Makefile`/`*.ps1`/`*.sh` wrappers.
- **Extract:**
  - The everyday commands — `dotnet build`, `dotnet run --project <…>`, `dotnet test`,
    `dotnet ef migrations add` / `database update`, `dotnet watch`. If Aspire is present, the run
    command comes from §4 instead.
  - **Which test runner is in play.** As of .NET 10, `dotnet test` runs **Microsoft.Testing
    Platform (MTP)** rather than VSTest when `global.json` contains
    `{"test": {"runner": "Microsoft.Testing.Platform"}}`. Other signals: `MSTest.Sdk`,
    `Microsoft.Testing.Platform*` packages, `<EnableMSTestRunner>`,
    `<TestingPlatformDotnetTestSupport>`, and `*.runsettings`. The runner changes which arguments
    the test command accepts, so record it — a plausible-looking wrong test command wastes an
    agent's whole loop.
  - The test framework (xUnit / NUnit / MSTest) plus the supporting libraries: mocking
    (NSubstitute / Moq), assertions (FluentAssertions / Shouldly), integration
    (`WebApplicationFactory`, Testcontainers), snapshot (Verify), coverage (coverlet /
    `Microsoft.CodeCoverage`).
  - **CLI convention.** .NET 10 added noun-first aliases (`dotnet package add`,
    `dotnet reference add`) alongside the verb-first forms (`dotnet add package`). Both work; note
    which the repo's scripts and docs use so an agent matches the house style. Same for one-shot
    tools (`dotnet tool exec` / `dnx`).
- **Record:** the 3–6 commands a developer actually runs, the runner and framework(s), and how
  tests are organized (by layer? by domain? separate integration/automation projects?).

## 9. Quality gates

- **Read:** `.editorconfig`; `.globalconfig`; analyzer packages in `*.csproj`
  (`StyleCop.Analyzers`, `Microsoft.CodeAnalysis.NetAnalyzers`, `SonarAnalyzer.CSharp`);
  arch-linting (`NsDepCop` config, `ArchUnitNET` test projects); `<TreatWarningsAsErrors>`,
  `<EnforceCodeStyleInBuild>`, `<AnalysisLevel>`; formatters (`dotnet format`, CSharpier).
- **Extract:** which gates are **present** vs **absent**.
- **Record:** a present/absent table. Where absent, add a recommendation (these are commonly
  missing): adopt `.editorconfig`, add Roslyn analyzers, add an arch-linting rule to enforce the
  layering found in §1.

## 10. UI & API surface

- **Read:** `.cshtml` (Razor Pages/MVC), `.razor` + `.razor.cs` (Blazor), `.xaml` + `.xaml.cs`
  (MAUI/WPF); `wwwroot/`; `Platforms/Android/` and `Platforms/iOS/`; endpoint definitions in
  `Program.cs` and `Controllers/`.
- **Extract:**
  - **API shape** — minimal APIs vs controllers, endpoint grouping, and the validation approach
    (ASP.NET Core 10 added built-in validation for minimal APIs).
  - **OpenAPI** — which generator: built-in `Microsoft.AspNetCore.OpenApi`
    (`AddOpenApi()` / `MapOpenApi()`), Swashbuckle, NSwag, or a UI layer like Scalar. Two details
    worth recording when the built-in one is used: it emits **OpenAPI 3.1 by default** in .NET 10
    (nullable types render as a type array including `null`, not `nullable: true`), and
    **`WithOpenApi()` is deprecated** — it still compiles but warns, so an agent should not add it
    to new endpoints. Swashbuckle left the default templates but is alive as a community project
    (v10.x supports .NET 10); record what the repo actually uses rather than what you expect.
  - **Identity** — ASP.NET Core Identity (including **passkey / WebAuthn** support, new in .NET 10
    and on by default in the Blazor Web App template), external identity
    (`Microsoft.Identity.Web` / Entra ID, Duende IdentityServer), or JWT bearer.
  - **Blazor** — render modes (Server / WebAssembly / Auto) and whether WASM preloading is on.
  - **MAUI** — the platform-specific service split (push notifications, analytics implemented per
    platform), renderers/handlers, deep-linking config.
- **Record:** the UI/API technology and any wiring an agent must respect. Omit this section
  entirely for headless services.

## 11. Deployment & packaging shape

Do not infer containerization from a `Dockerfile` alone — a modern .NET repo can ship container
images with no Dockerfile at all.

- **Read:** `Dockerfile` / `compose*.yml`; container-related MSBuild properties; publish-related
  properties; CI publish steps.
- **Extract:**
  - **SDK container publishing** — `dotnet publish /t:PublishContainer`, plus
    `<EnableSdkContainerSupport>`, `<ContainerRepository>`, `<ContainerImageName>`, and
    `<ContainerImageFormat>` (`Docker` | `OCI`, new in .NET 10). In .NET 10 console apps can
    publish images without setting any property.
  - **Native AOT / trimming / single file** — `<PublishAot>`, `<PublishTrimmed>`, `<TrimMode>`,
    `<PublishSingleFile>`, `<PublishReadyToRun>`, `<InvariantGlobalization>`.
  - **Output layout** — `<ArtifactsPath>` or `--artifacts-path`, which moves build output out of
    the per-project `bin/`/`obj/`.
- **Record:** how an image or binary is actually produced. The AOT/trimming flags belong in
  **gotchas**, not just facts: they are hard constraints on what an agent may write (no unbounded
  reflection, no reflection-based JSON serialization — use source-generated
  `JsonSerializerContext`).

## 12. Cross-cutting concerns

- **Read/Grep:** observability packages (`OpenTelemetry.*`,
  `Microsoft.Extensions.Diagnostics.HealthChecks`, `Serilog`, `NLog`); resilience
  (`Microsoft.Extensions.Http.Resilience`, Polly v8 pipelines); AI-shaped signals
  (`Microsoft.Extensions.AI`, `Microsoft.SemanticKernel`, `ModelContextProtocol`, a `.mcp.json`
  at the repo root).
- **Extract:** what is wired up and where.
- **Record:** one short list. **If Aspire is present (§4), say so here** — ServiceDefaults usually
  already supplies telemetry, health checks, and resilience, and an agent should extend that rather
  than register its own per service.

## 13. Gotchas / hotspots

- **Read/Grep:** unusually large files (e.g. `wc -l` on `*.cs`), high-churn files if git history is
  available, `async` methods, `CancellationToken` usage, `// TODO` / `// FIXME` density.
- **Extract:** monolithic classes (a 9k-LOC `DbContext`, a 2k-LOC service), async/cancellation
  conventions (are cancellation tokens threaded through?), and notable TODO clusters.
- **C# language posture.** Grep for the C# 14 idioms the repo has adopted: the `field` contextual
  keyword in properties, `extension` blocks, null-conditional assignment (`?.=`), partial
  constructors and events, user-defined compound/increment operators. Two reasons to record what
  you find: it sets a language floor (C# 14 needs the .NET 10 SDK), and an agent should **match the
  repo's idiom rather than introduce it** — sprinkling `field`-backed properties into a codebase
  that uses explicit backing fields is noise, not modernization.
- **Record:** a short "gotchas" list — the non-obvious things that will trip up an agent editing
  this solution. Fold in the AOT/trimming constraints from §11 if they apply.

## 14. File-based apps (skip if absent)

Easy to miss entirely, because a `*.csproj` glob never sees them.

- **Read/Grep:** standalone `.cs` files with `#:package`, `#:sdk`, `#:project`, or `#:property`
  directives; `#!/usr/bin/env dotnet` shebangs; extensionless executable files whose first line is
  that shebang.
- **Extract:** what each script does and what it references.
- **Record:** these as part of the repo's tooling surface — they run with `dotnet run app.cs`, they
  **target native AOT by default** in .NET 10 (unless `#:property PublishAot=false`), they can be
  published to a native executable with `dotnet publish app.cs`, and `dotnet project convert`
  promotes one into a real project when it outgrows a single file.
