# AGENTS.md — TheOffice

Ecommerce B2B de artículos de oficina. Hoy existen el **backend del catálogo** (`src/`): API REST de productos, categorías y clientes, y el **frontend del catálogo** (`src/Presentation/TheOffice.Web`): dos pantallas Angular de solo lectura. Carrito/pedidos, backoffice y autenticación **no están construidos** — no asumas que existen.

Guía para agentes de IA que trabajan en este repositorio. Sigue la convención [agents.md](https://agents.md).

Este archivo solo captura lo que no es obvio leyendo el código. Para arquitectura, modelo de datos, decisiones y contexto más amplio, sigue los enlaces y lee la fuente.

## Dónde encontrar las cosas

- [`docs/dotnet.md`](./docs/dotnet.md) — contexto .NET profundo: grafo de proyectos, TFMs, paquetes, EF Core, DI, trampas. **Empieza aquí.**
- [`docs/architecture.md`](./docs/architecture.md) — las capas, el patrón y los invariantes arquitectónicos.
- [`docs/data-model.md`](./docs/data-model.md) — esquema, relaciones y flujo de migraciones.
- [`docs/infrastructure.md`](./docs/infrastructure.md) — desarrollo local, configuración y (falta de) ruta a producción.
- [`docs/business.md`](./docs/business.md) — qué es el producto y quién paga.
- [`docs/target-user.md`](./docs/target-user.md) — quién lo usa y qué le importa.
- [`docs/adrs/`](./docs/adrs/) — por qué las cosas son como son.
- [`src/Presentation/TheOffice.Web/README.md`](./src/Presentation/TheOffice.Web/README.md) — el frontend Angular: cómo correrlo, y el mapeo de tokens del diseño a Tailwind.
- [`docs/claims-ledger.md`](./docs/claims-ledger.md) — qué de estos docs está verificado contra el repo.
- [`README.md`](./README.md) — doc preexistente: endpoints, stack y las decisiones de implementación originales.

Lee estos docs antes de hacer cambios estructurales.

## Comandos

Todo pasa por el `Makefile` en la raíz del repo. `make help` lista los objetivos disponibles.

```bash
make hooks     # primero, una sola vez: instala los git hooks (Lefthook)
make check     # lint + build + test — la señal única de que el repo está bien
make run       # levanta la API en http://localhost:5226 · docs en /scalar
```

Cómo instalar `make`, Lefthook y gitleaks está en el [`README.md`](./README.md).

Objetivos más finos cuando no hace falta la comprobación completa:

```bash
make build     # compilación estricta (los warnings son errores)
make test      # todas las pruebas, unitarias y de arquitectura
make arch      # solo las pruebas de arquitectura
make lint      # verifica el estilo, sin modificar nada
make format    # corrige el estilo en el sitio
make secrets   # escanea el árbol de trabajo en busca de secretos
make audit     # reporta dependencias vulnerables (solo informa, nunca falla)
```

Las migraciones siguen siendo un comando de EF Core, desde `src/`
(requiere `dotnet tool install --global dotnet-ef`):

```bash
dotnet ef migrations add <Nombre> -p Infrastructure/TheOffice.Persistence -s Presentation/TheOffice.Api
```

En `Development` la app aplica migraciones y siembra datos al arrancar — no hay que preparar la BD.

## Comprobaciones a ejecutar

```bash
make check    # señal única de confianza: lint + build + test
```

### Qué comprobación para qué cambio

- **Cualquier cambio en `.cs`:** `make check`
- **Prueba nueva o modificada:** `make test`
- **Estilo o imports:** `make lint` (y `make format` para corregirlo)
- **Cambio de capa o de arquitectura:** `make test` — lo verifican las pruebas de arquitectura
- **Dependencia NuGet:** `make build` — la auditoría de NuGet corre sola
- **Cambio en `scripts/agent-hooks/`:** dispáralo a mano, y sincroniza la plantilla de la skill
  `instrument-agent-dotnet`, que hoy es el mismo archivo con otro encabezado

## Integración continua

GitHub Actions, en [`.github/workflows/ci.yml`](./.github/workflows/ci.yml). Corre en **cada push
de cada rama**, no solo en `main`: una compuerta que solo actúa después del merge avisa cuando el
problema ya es de todos. El workflow instala el SDK desde `global.json` y ejecuta `make ci` — no
repite los pasos, para que no se desincronice del `Makefile`. `make ci` es `make check` más
restauración en modo bloqueado y el escaneo de secretos.

El pipeline es un reporte hasta que se exija como status check obligatorio en la protección de
rama. Ver [infraestructura](./docs/infrastructure.md).

## Hooks del agente

`.claude/settings.json` registra dos hooks, implementados en `scripts/agent-hooks/`. Corren
**solo bajo Claude Code**: los scripts son shell portable, pero el registro no lo lee ningún otro
agente. No son una frontera de seguridad — corren con tu shell y tus permisos, y leen el comando,
no la intención: un nombre que el comando construye en tiempo de ejecución les pasa por debajo.

- **`secret-read-guard.sh`** — `PreToolUse`. **Bloquea** leer archivos de credenciales: `.env`,
  llaves privadas (`*.pem`, `*.pfx`, `id_rsa`), `secrets.json`, `.ssh/`, `.aws/credentials`. Si te
  lo rechaza al abrir uno de verdad, usa el `.example` versionado o pide el valor al usuario —
  no es un bug. Solo mira los operandos que se abren: un `echo`, un mensaje de commit o un
  heredoc que **mencionen** el archivo pasan. Un rechazo sobre algo que no abre nada sí es un bug.
- **`format-on-edit.sh`** — `PostToolUse`. Corre `dotnet format` sobre el `.cs` recién escrito, y
  solo sobre ese archivo. **No gastes turnos en indentación ni en el orden de los `using`.** Nunca
  bloquea.

## MCP

`.mcp.json` registra dos servidores, para que el agente consulte los sistemas en vez de recibirlos
pegados en el chat:

- **`mslearn`** — documentación oficial de .NET, EF Core y Azure. HTTP, sin credenciales.
- **`dbhub`** — lectura del esquema y los datos reales de la base SQLite. Necesita la variable de
  entorno `APP_DSN`; su valor nunca se versiona. Ver el [`README.md`](./README.md) para exportarla
  y para activar los servidores.

## Reglas no obvias

**Arquitectura y contratos**

- **Las dependencias apuntan hacia adentro.** `Domain` no referencia nada; `Application` define los puertos e `Infrastructure` los implementa; los controllers no tienen lógica de negocio. **Ahora lo verifica `tests/TheOffice.ArchitectureTests`**: romper la dirección de dependencias hace fallar `make test`. Ver [arquitectura](./docs/architecture.md).
- **Result pattern, nunca excepciones para control de flujo.** Fallas esperadas → `Result.Failure(...)`. Justificación: [ADR-0003](./docs/adrs/adr-0003-result-pattern.md).
- **Las rutas y requests usan `PublicId` (`PRD-001`), nunca el `Id` interno (Guid).** Es una decisión de seguridad, no un detalle.
- **Hay dos jerarquías de modelos con los mismos nombres**: `Domain/Entities/*` y `Persistence/Models/*`. Son clases distintas; la traducción vive en los mappers de cada capa. Nunca pases una donde se espera la otra.
- **Los métodos async no llevan sufijo `Async`** (`GetPaged`, no `GetPagedAsync`). Es deliberado.

**Trampas mecánicas**

- **Tocar un seeder exige generar una migración.** Los datos semilla van por `HasData` dentro de `OnModelCreating`; cambiarlos sin migración deja la BD desincronizada.
- **No elimines `SQLitePCLRaw.bundle_e_sqlite3` ni `Microsoft.OpenApi`** de los `.csproj`. Parecen no usados: existen solo para elevar versiones transitivas con CVE, y está comentado en el archivo.
- **`Database.Migrate()` solo corre en `Development`.** Cualquier otro entorno necesita aplicar migraciones por fuera.
- **No borres `global.json`.** Fija el SDK (`10.0.100` con `rollForward: latestFeature`) y selecciona Microsoft.Testing.Platform, que exige `xunit.v3.mtp-v2`. No asumas flags de VSTest en `dotnet test`: para escribir un TRX este runner usa `--report-xunit-trx`.
- **Las versiones de paquetes son centralizadas.** Viven en `Directory.Packages.props` en la raíz, así que un `<PackageReference>` **nunca lleva** atributo `Version` — solo `Include`. Para agregar o subir un paquete, edita el `<PackageVersion>` en ese archivo. `VersionOverride` está deshabilitado a propósito: un proyecto que necesite otra versión lo plantea ahí, no lo resuelve por su cuenta.
- **Cambiar una versión exige `dotnet restore` en el mismo commit.** Cada proyecto tiene un `packages.lock.json` versionado; CI restaura en modo bloqueado y falla si el lock y el proyecto no coinciden (error `NU1004`).
- **Los warnings son errores.** `TreatWarningsAsErrors` está activo en `Directory.Build.props`: un `using` sin usar (`IDE0005`) o una variable asignada y nunca leída (`CS0219`) rompen la compilación, no la ensucian.
- **El estilo se verifica dentro del build.** `EnforceCodeStyleInBuild` hace que las reglas del `.editorconfig` corran al compilar. Si `make lint` se queja, `make format` lo arregla.
- **Los hooks corren al hacer commit.** Formato y escaneo de secretos en `pre-commit`, formato del mensaje en `commit-msg`, `make check` en `pre-push`. Salida de emergencia: `LEFTHOOK=0 git commit ...`, y úsala sabiendo lo que te estás saltando.
- **En `.editorconfig`, toda regla de C# va en la sección `[*.{cs,csx}]`.** El archivo no tiene más alcance que el último encabezado leído: una regla escrita después de las secciones de código generado aplica solo a esos archivos, que además están excluidos — queda inerte y nada lo reporta.
- **`HasConversion<double>()` en `Price` es un workaround solo de SQLite** (no puede ordenar `decimal`). Se retira al migrar a SQL Server; no lo copies a otros campos decimales. Ver [ADR-0002](./docs/adrs/adr-0002-persistencia-sqlite-ef-core.md).

**Huecos deliberados — no los "arregles" por tu cuenta**

- **No agregues autenticación.** Los endpoints de escritura están abiertos a propósito (punto 4 del roadmap).
- **No introduzcas `CancellationToken`.** Cero ocurrencias en todo `src/`: sigue el patrón existente en vez de hilarlo en un solo método.
- **No metas una librería de validación** (FluentValidation, DataAnnotations en los DTOs). Hoy la validación vive a mano en los servicios; cambiarlo es una decisión de arquitectura, no un fix.

## Pruebas

xUnit v3 + NSubstitute, con las aserciones nativas de `Assert` (los DTOs de respuesta son `record`, así que `Assert.Equal` compara estructuralmente). Un proyecto de pruebas por proyecto bajo prueba, en `tests/`, con archivos que espejan el layout de `src/`. Los nombres de prueba van en **inglés**, como el resto del código: `Method_Scenario_ExpectedResult`. Las pruebas actuales de `ProductServiceTests` todavía están en español; renómbralas cuando toques el archivo.

`tests/TheOffice.ArchitectureTests` es la excepción a la regla de un proyecto de pruebas por proyecto bajo prueba: no prueba una capa, sino la relación entre todas. Usa ArchUnitNET y convierte la regla de dependencias de [arquitectura](./docs/architecture.md) en una prueba que se pone en rojo. `make test` lo corre junto con el resto; `make arch` lo corre solo.

Un cambio se considera listo cuando tiene **pruebas unitarias de la capa Application y pruebas de integración** para repositorios y controllers. Hoy hay unitarias de `ProductService` y las de arquitectura; la integración (`WebApplicationFactory`, SQLite in-memory) aún no está montada y `Program.cs` no expone `public partial class Program`, que haría falta. Ver [`docs/dotnet.md`](./docs/dotnet.md) §7 y §12.

## Frontend (`src/Presentation/TheOffice.Web`)

Angular 22 + Tailwind v4. Dos pantallas de solo lectura: listado (`/`, `/productos`) y ficha
(`/productos/:publicId`). Su [`README`](./src/Presentation/TheOffice.Web/README.md) tiene el detalle
y el mapeo completo de tokens; aquí solo va lo que no se deduce leyendo el código.

- **No está en `src/TheOffice.sln`** y no tiene `.csproj` envoltorio. Es **invisible** para
  `dotnet build`, `dotnet format` y las pruebas de arquitectura — que es justo lo que se busca. Toda
  su integración con las compuertas pasa por el `Makefile` (`make web-*`, y `make check` los corre).
- **`make check` ahora exige Node** y paga un `npm ci` en cada corrida, incluso para un cambio de una
  línea en C#. Es el costo aceptado de tener una sola señal de confianza. La versión está en
  `src/Presentation/TheOffice.Web/.nvmrc`; el CLI **rechaza** versiones anteriores, no avisa.
- **`pageSize` es 10 por decisión del frontend**, no el 6 que la API devuelve por defecto. Va
  explícito en cada petición.
- **Los nombres de categoría llegan sin tildes** (`Papeleria`, `Tecnologia`). Se renderiza lo que
  llega: no se "corrige" en el cliente.
- **La búsqueda por SKU** (`^PRD-\d{3}$` → navegar a la ficha) **vive solo en el cliente.** La API no
  la tiene y no se le va a pedir.
- **Cero valores arbitrarios de Tailwind** (`bg-[#…]`): si falta un color, falta un token en el
  bloque `@theme` de `src/styles.css`. Tailwind v4 no usa `tailwind.config.js`.
- **Los fallos son valores, no excepciones**: `CatalogService` devuelve `Fetched<T>`
  (`ok` / `not-found` / `error`), mismo criterio que el Result pattern del backend. La pantalla
  nunca muestra un código HTTP.
- **Los huecos deliberados también aplican aquí**: sin carrito, sin autenticación, sin i18n, sin
  selector de ordenamiento, sin contadores por categoría, ninguna mención a IVA. Si el diseño lo
  pide, está fuera de alcance.
- Las pruebas corren con **Vitest sin navegador**, para que CI no dependa de Chrome. El recorrido
  contra la app real está en
  [`docs/plans/frontend-validation-script.md`](./docs/plans/frontend-validation-script.md).

## Estilo de código

El estilo **sí se verifica**: `.editorconfig` en la raíz, analizadores de .NET activos y `TreatWarningsAsErrors`. `make lint` es la compuerta y `make format` la corrige. Ya no hace falta deducir el estilo del archivo que estás tocando — si pasa `make check`, está bien.

Lo que el `.editorconfig` fija: **indentación de 2 espacios**, `using` ordenados y separados por grupos, namespaces con ámbito de archivo, campos privados con `_camelCase`, y llave de apertura en su propia línea. `Nullable` e `ImplicitUsings` están habilitados en `Directory.Build.props` para toda la solución. Las properties auto-implementadas con constructores explícitos siguen siendo convención, no regla. El repo **no** usa idioms de C# 14 (`field`, bloques `extension`, `?.=`); no los introduzcas.

## Seguridad

- No commitees `.env` ni archivos con credenciales. Hoy no hay secretos en el repo: la única cadena de conexión es un archivo SQLite local, ignorado por Git.
- En producción los secretos vienen de **variables de entorno** (`ConnectionStrings__DefaultConnection`), no de `appsettings.json`.
- **`Cors:AllowedOrigins` vacío hace que la API caiga en `AllowAnyOrigin()`.** Cómodo en local; al desplegar hay que poblarlo.
- No registres secretos, tokens ni información personal en logs.
- Asume que cualquier cosa en este repo es legible por un agente de IA — nunca pegues secretos aquí.
