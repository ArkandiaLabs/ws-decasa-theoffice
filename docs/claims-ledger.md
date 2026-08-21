# Registro de afirmaciones

Generado por la skill `agent-context-dotnet`. Registra las afirmaciones factuales clave de la
documentación, su fuente en el repositorio y si fueron confirmadas por una persona. Vuelve a
ejecutar la skill para actualizarlo.

Fecha de generación: 2026-08-20 · rama `session-02` · commit `cd1ca3b`.

## Verificadas contra archivos del repo

| Afirmación | Fuente | Confianza | Estado |
|---|---|---|---|
| Los seis proyectos usan `net10.0`, sin multi-targeting. | `src/**/*.csproj` (ej. `TheOffice.Api.csproj:4`) | alta | confirmada |
| La persistencia es SQLite vía EF Core 10.0.10. | `TheOffice.Persistence.csproj:9`, `Persistence/DependencyInjection.cs:15` | alta | confirmada |
| `Price` se persiste como `double` con `HasConversion<double>()`. | `TheOfficeDbContext.cs:31` | alta | confirmada |
| `Category` 1—N `Product` con `DeleteBehavior.Restrict`. | `TheOfficeDbContext.cs:37` | alta | confirmada |
| Las migraciones se aplican al arrancar **solo** en `Development`. | `Program.cs:58,62` | alta | confirmada |
| El runner de pruebas es Microsoft.Testing.Platform, fijado en `global.json`. | `global.json:3` | alta | confirmada |
| Las pruebas usan xUnit v3 (`xunit.v3.mtp-v2` 3.2.2) + NSubstitute 6.0.0. | `TheOffice.Application.Tests.csproj:21-22` | alta | confirmada |
| `Cors:AllowedOrigins` vacío hace que la política caiga en `AllowAnyOrigin()`. | `Program.cs:36,44` | alta | confirmada |
| Servicios, repositorios y `DbContext` se registran todos como `Scoped`. | `*/DependencyInjection.cs` | alta | confirmada |
| Los servicios de Application se registran como clases **concretas**, sin interfaz. | `Application/DependencyInjection.cs:11-13` | alta | confirmada |
| El filtro `IsActive` es un `.Where()` explícito en `GetPaged`, no un query filter global. | `ProductRepository.cs:54` | alta | confirmada |
| `SQLitePCLRaw.bundle_e_sqlite3` y `Microsoft.OpenApi` son referencias directas para elevar transitivas con CVE. | `TheOffice.Persistence.csproj:10`, `TheOffice.Api.csproj:14` (comentarios) | alta | confirmada |
| Ningún método async propaga `CancellationToken`. | `grep -r CancellationToken src/` → 0 resultados | alta | confirmada |
| `GlobalConstants.TheOfficeDbContext` no tiene referencias en el repo. | `grep -r GlobalConstants` → 0 usos | alta | confirmada |
| `ProductController` no es testeable unitariamente (depende de la clase concreta `ProductService`, sin métodos `virtual`). | `ProductController.cs:13`, `grep -r virtual src/` → 0 resultados | alta | confirmada |
| `Program.cs` no expone `public partial class Program`, requisito de `WebApplicationFactory`. | `grep 'partial class Program'` → 0 resultados | alta | confirmada |
| No hay gestión centralizada de paquetes (sin `Directory.Packages.props`). | ausencia de archivo | alta | confirmada |
| No hay `.editorconfig`, analizadores Roslyn, arch-linting ni `TreatWarningsAsErrors`. | ausencia de archivos y propiedades | alta | confirmada |
| No hay CI, `Dockerfile`, IaC ni `CODEOWNERS`. | ausencia de `.github/`, `Dockerfile`, `*.tf`, `CODEOWNERS` | alta | confirmada |
| La API usa controllers con rutas versionadas `api/v{version:apiVersion}/...`, solo v1.0. | `Controllers/*.cs`, `Program.cs:23-35` | alta | confirmada |
| La documentación (OpenAPI + Scalar) solo se monta en `Development`. | `Program.cs:58,66-67` | alta | confirmada |
| No hay autenticación ni autorización en ningún endpoint. | ausencia de `[Authorize]` y de paquetes de auth | alta | confirmada |
| No hay observabilidad (OpenTelemetry, health checks, Serilog/NLog). | ausencia en todos los `.csproj` | alta | confirmada |
| El proyecto de pruebas vive en `tests/`, fuera de `src/`, registrado en la solución con ruta relativa. | `src/TheOffice.sln` | alta | confirmada |
| Solo `ProductService` tiene pruebas (11 métodos, 16 casos con los `InlineData`). | `tests/**/ProductServiceTests.cs` | alta | confirmada |
| La app escucha en `http://localhost:5226`. | `launchSettings.json:17` | alta | confirmada |

## Inferidas del ecosistema .NET (no leídas de este repo)

| Afirmación | Fuente | Confianza | Estado |
|---|---|---|---|
| Sin `<LangVersion>` declarado, el SDK de .NET 10 aplica C# 14 por defecto. | inferido (defaults del SDK) | media | sin objeción |
| El generador integrado de OpenAPI emite OpenAPI 3.1 por defecto en .NET 10. | inferido (comportamiento del framework) | media | sin objeción |
| `WithOpenApi()` está deprecado en .NET 10. | inferido (comportamiento del framework) | media | sin objeción |

Ninguna de las tres es específica de este repositorio; si el comportamiento del framework cambia,
corrige el doc, no el código.

## Aportadas por el equipo (no verificables contra el repo)

| Afirmación | Fuente | Confianza | Estado |
|---|---|---|---|
| Los clientes principales son departamentos de compras y secretarias (venta B2B). | usuario | — | confirmada por el equipo |
| Es un producto real en construcción, no un repo de formación. | usuario | — | confirmada por el equipo |
| Un cambio está "listo" con pruebas unitarias **y** de integración. | usuario | — | confirmada por el equipo |
| En producción los secretos vendrán de variables de entorno. | usuario | — | intención, no implementado |
| La intención es que CI despliegue al hacer merge a `main`. | usuario | — | intención, **no implementado** |
| El destino de despliegue aún no está definido. | usuario | — | confirmada por el equipo |
| Los PR los revisa el equipo de Arkandia (Manuel Zapata y David Lopera). | usuario | — | confirmada por el equipo |
| Reglas: tocar un seeder exige migración · no quitar los paquetes anti-CVE · `Migrate()` solo en Development · no agregar auth · no introducir `CancellationToken` · no meter librería de validación. | usuario (candidatos detectados en código) | — | confirmadas por el equipo |

## Abiertas / corregidas

| Afirmación | Fuente | Confianza | Estado |
|---|---|---|---|
| Los precios están en pesos colombianos. | inferido de magnitudes en `Seeders/ProductSeeder.cs` | baja | **corregida** → el doc ya no lo afirma; `docs/data-model.md` lleva un `TODO: verificar`. El modelo no tiene campo `Currency`. |
| El SDK usado es 10.0.301. | `dotnet --version` en una máquina, no en el repo | baja | **corregida** → los docs ahora dicen "cualquier SDK de .NET 10". |
| `dotnet-ef` no está instalada. | `dotnet tool list --global` en una máquina | baja | **corregida** → los docs dicen que hay que instalarla como herramienta global, sin afirmar el estado de ninguna máquina. |
| El comportamiento de compra del usuario objetivo (quién aprueba, por qué canal se cierra el pedido, peso del precio vs. entrega). | inferido del segmento declarado | baja | **abierta** → marcada como `TODO` en `docs/target-user.md`; requiere research con usuarios. |
| Modelo de ingresos, márgenes y condiciones de pago corporativo. | no disponible | — | **abierta** → `TODO` en `docs/business.md`. |
| Topología productiva y pipeline de CI/CD. | no existen todavía | — | **abierta** → `TODO` en `docs/infrastructure.md`. |
