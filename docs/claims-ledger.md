# Registro de afirmaciones

Generado por la skill `agent-context-dotnet`. Registra las afirmaciones factuales clave de la
documentación, su fuente en el repositorio y si fueron confirmadas por una persona. Vuelve a
ejecutar la skill para actualizarlo.

Fecha de generación: 2026-08-20 · rama `session-02` · commit `cd1ca3b`.
Actualizado: 2026-08-25 · rama `session-03` · tras instalar la instrumentación determinista.
Actualizado: 2026-08-25 · rama `session-03` · tras instalar la instrumentación no determinista
(hooks del agente y servidores MCP).

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
| La gestión de paquetes es centralizada: todas las versiones en `Directory.Packages.props`, ningún `<PackageReference>` con `Version`. | `Directory.Packages.props`, `src/**/*.csproj` | alta | confirmada |
| Cada uno de los siete proyectos tiene un `packages.lock.json` versionado; `dotnet restore --locked-mode` falla con `NU1004` si derivan. | `**/packages.lock.json`; verificado rompiendo una versión a propósito | alta | confirmada |
| `global.json` fija el SDK en `10.0.100` con `rollForward: latestFeature`. | `global.json:3-4`; verificado pidiendo `10.0.500` y viendo fallar el build | alta | confirmada |
| `TreatWarningsAsErrors`, `EnableNETAnalyzers` y `EnforceCodeStyleInBuild` están activos para toda la solución. | `Directory.Build.props`; verificado con una variable sin usar → `CS0219` como error | alta | confirmada |
| Existe `.editorconfig` con indentación de 2 espacios, medida sobre los `.cs` existentes. | `.editorconfig`; 407 líneas con sangría de 2, 0 con tabuladores | alta | confirmada |
| `make check` (estilo + compilación + pruebas) pasa en verde: 25 pruebas, 0 fallos. | ejecución de `make check` | alta | confirmada |
| El arch-linting existe: `tests/TheOffice.ArchitectureTests`, 9 reglas con ArchUnitNET 0.13.4. | `tests/TheOffice.ArchitectureTests/*.cs`; verificado metiendo EF Core en `CategoryService` y viendo fallar la regla | alta | confirmada |
| Los hooks de Git bloquean commits con formato inválido o secretos. | `lefthook.yml`; verificado intentando commits reales, que no se escribieron | alta | confirmada |
| Hay CI en GitHub Actions que corre `make ci` en cada push de cada rama e instala el SDK desde `global.json`. | `.github/workflows/ci.yml` | alta | confirmada (por inspección; **no ha corrido nunca todavía**) |
| La historia del repo no contiene secretos. | `gitleaks detect` sobre los 5 commits | alta | confirmada |
| No hay CI, `Dockerfile`, IaC ni `CODEOWNERS`. | ausencia de `.github/`, `Dockerfile`, `*.tf`, `CODEOWNERS` | alta | confirmada |
| La API usa controllers con rutas versionadas `api/v{version:apiVersion}/...`, solo v1.0. | `Controllers/*.cs`, `Program.cs:23-35` | alta | confirmada |
| La documentación (OpenAPI + Scalar) solo se monta en `Development`. | `Program.cs:58,66-67` | alta | confirmada |
| No hay autenticación ni autorización en ningún endpoint. | ausencia de `[Authorize]` y de paquetes de auth | alta | confirmada |
| No hay observabilidad (OpenTelemetry, health checks, Serilog/NLog). | ausencia en todos los `.csproj` | alta | confirmada |
| El proyecto de pruebas vive en `tests/`, fuera de `src/`, registrado en la solución con ruta relativa. | `src/TheOffice.sln` | alta | confirmada |
| Solo `ProductService` tiene pruebas (11 métodos, 16 casos con los `InlineData`). | `tests/**/ProductServiceTests.cs` | alta | confirmada |
| La app escucha en `http://localhost:5226`. | `launchSettings.json:17` | alta | confirmada |
| `.claude/settings.json` registra dos hooks: `secret-read-guard` (`PreToolUse`) y `format-on-edit` (`PostToolUse`). | `.claude/settings.json`, `scripts/agent-hooks/` | alta | confirmada |
| El guardia de secretos **bloquea** leer un `.env`, y deja pasar un archivo normal. | verificado disparándolo: `Read` sobre `hooktest/.env` rechazado, `Read` sobre `global.json` permitido | alta | confirmada |
| El guardia distingue un archivo que se abre de un nombre que solo se menciona: deniega `cat .env`, un operando entre comillas, una redirección y una asignación; permite `echo`, `printf`, `-m`/`--message`, cuerpos de heredoc y here-strings que nombren el archivo. | 39 casos ejecutados contra el hook con payloads reales, sobre `scripts/agent-hooks/` y sobre la plantilla de la skill; 39/39 | alta | confirmada |
| Los tres comandos que el guardia bloqueó por error en la sesión del 2026-08-25 hoy pasan. | reejecutados verbatim contra el hook, y versionados como regresión en el suite | alta | confirmada |
| `make hooks-test` corre el suite del guardia, y `check` y `ci` lo incluyen. | `Makefile`, `scripts/agent-hooks/tests/` | alta | confirmada |
| El formateo automático deja el `.cs` recién escrito pasando la misma compuerta que `make lint`. | verificado escribiendo un `.cs` con `using` sin ordenar e indentación errónea; `dotnet format --verify-no-changes` sobre él salió 0 | alta | confirmada |
| `dotnet format --include <ruta relativa a la raíz>` sí acota al archivo, ejecutado desde la raíz del repo. | verificado con el mismo archivo de prueba | alta | confirmada |
| Formatear un solo archivo cuesta ~3,3 s; `make audit` cuesta ~7,2 s y hoy no reporta paquetes vulnerables. | medidos en esta máquina | media | confirmada (una sola máquina) |
| `.gitignore` ignora los archivos de credenciales (`.env`, `.env.*`, `appsettings.Secrets.json`), con `!.env.example` como excepción. | `.gitignore`, sección «Credenciales locales» | alta | confirmada |
| `.mcp.json` registra `mslearn` (HTTP) y `dbhub` (stdio, `@bytebase/dbhub@1.2.1`), sin credenciales literales. | `.mcp.json` | alta | confirmada |
| `@bytebase/dbhub@1.2.1` acepta `--transport stdio` y `--dsn`, y conecta contra el SQLite local. | ejecutado con el DSN real; salida `Tool registry initialized`, exit 0 | alta | confirmada |

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
| El SDK usado es 10.0.301. | `dotnet --version` en una máquina, no en el repo | baja | **corregida** → `global.json` ahora fija `10.0.100` con `rollForward: latestFeature`, que es una restricción del repo y no de una máquina. |
| `dotnet-ef` no está instalada. | `dotnet tool list --global` en una máquina | baja | **corregida** → los docs dicen que hay que instalarla como herramienta global, sin afirmar el estado de ninguna máquina. |
| El comportamiento de compra del usuario objetivo (quién aprueba, por qué canal se cierra el pedido, peso del precio vs. entrega). | inferido del segmento declarado | baja | **abierta** → marcada como `TODO` en `docs/target-user.md`; requiere research con usuarios. |
| Modelo de ingresos, márgenes y condiciones de pago corporativo. | no disponible | — | **abierta** → `TODO` en `docs/business.md`. |
| Topología productiva y pipeline de despliegue. | no existen todavía | — | **abierta** → `TODO` en `docs/infrastructure.md`. El pipeline de **calidad** ya existe; el de **despliegue** no, porque no hay destino definido. |
| Que el pipeline de CI efectivamente pase en el runner de GitHub. | escrito pero nunca ejecutado | media | **abierta** → se resuelve con el primer push. |
| Que `check` sea un status check obligatorio en la protección de rama de `main`. | requiere configuración en GitHub, fuera del repo | alta | **abierta** → hasta entonces CI reporta, no bloquea. |
| Que los servidores MCP de `.mcp.json` conecten en una sesión real. | el archivo está escrito y `dbhub` arranca a mano, pero los servidores quedan en `⏸ Pending approval` hasta que se confíe en el espacio de trabajo | alta | **abierta** → escritos, pendientes de aprobación. Se resuelve con `claude` en el repo + `/mcp`. |
| Que los hooks funcionen sobre Git Bash en Windows. | escritos para bash 3.2 sin `jq`, pero solo ejecutados en macOS | media | **abierta** → nadie del equipo trabaja en Windows hoy. |
