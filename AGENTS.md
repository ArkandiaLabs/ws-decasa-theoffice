# AGENTS.md — TheOffice

Ecommerce B2B de artículos de oficina. Hoy solo existe el **backend del catálogo** (`src/`): API REST de productos, categorías y clientes. Frontend, carrito/pedidos, backoffice y autenticación **no están construidos** — no asumas que existen.

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
- [`docs/claims-ledger.md`](./docs/claims-ledger.md) — qué de estos docs está verificado contra el repo.
- [`README.md`](./README.md) — doc preexistente: endpoints, stack y las decisiones de implementación originales.

Lee estos docs antes de hacer cambios estructurales.

## Comandos

```bash
# desde src/
dotnet restore
dotnet build TheOffice.sln
dotnet run --project Presentation/TheOffice.Api   # http://localhost:5226 · docs en /scalar

# desde la raíz del repo (global.json vive ahí)
dotnet test src/TheOffice.sln

# migraciones, desde src/ (requiere: dotnet tool install --global dotnet-ef)
dotnet ef migrations add <Nombre> -p Infrastructure/TheOffice.Persistence -s Presentation/TheOffice.Api
```

En `Development` la app aplica migraciones y siembra datos al arrancar — no hay que preparar la BD.

## Reglas no obvias

**Arquitectura y contratos**

- **Las dependencias apuntan hacia adentro.** `Domain` no referencia nada; `Application` define los puertos e `Infrastructure` los implementa; los controllers no tienen lógica de negocio. Nada verifica esto — no hay arch-linting.
- **Result pattern, nunca excepciones para control de flujo.** Fallas esperadas → `Result.Failure(...)`. Justificación: [ADR-0003](./docs/adrs/adr-0003-result-pattern.md).
- **Las rutas y requests usan `PublicId` (`PRD-001`), nunca el `Id` interno (Guid).** Es una decisión de seguridad, no un detalle.
- **Hay dos jerarquías de modelos con los mismos nombres**: `Domain/Entities/*` y `Persistence/Models/*`. Son clases distintas; la traducción vive en los mappers de cada capa. Nunca pases una donde se espera la otra.
- **Los métodos async no llevan sufijo `Async`** (`GetPaged`, no `GetPagedAsync`). Es deliberado.

**Trampas mecánicas**

- **Tocar un seeder exige generar una migración.** Los datos semilla van por `HasData` dentro de `OnModelCreating`; cambiarlos sin migración deja la BD desincronizada.
- **No elimines `SQLitePCLRaw.bundle_e_sqlite3` ni `Microsoft.OpenApi`** de los `.csproj`. Parecen no usados: existen solo para elevar versiones transitivas con CVE, y está comentado en el archivo.
- **`Database.Migrate()` solo corre en `Development`.** Cualquier otro entorno necesita aplicar migraciones por fuera.
- **No borres `global.json`.** Selecciona Microsoft.Testing.Platform, que exige `xunit.v3.mtp-v2`. Y no asumas flags de VSTest en `dotnet test`.
- **Las versiones de paquetes van por proyecto.** No hay `Directory.Packages.props`, así que cada `<PackageReference>` **sí lleva** su atributo `Version`.
- **`HasConversion<double>()` en `Price` es un workaround solo de SQLite** (no puede ordenar `decimal`). Se retira al migrar a SQL Server; no lo copies a otros campos decimales. Ver [ADR-0002](./docs/adrs/adr-0002-persistencia-sqlite-ef-core.md).

**Huecos deliberados — no los "arregles" por tu cuenta**

- **No agregues autenticación.** Los endpoints de escritura están abiertos a propósito (punto 4 del roadmap).
- **No introduzcas `CancellationToken`.** Cero ocurrencias en todo `src/`: sigue el patrón existente en vez de hilarlo en un solo método.
- **No metas una librería de validación** (FluentValidation, DataAnnotations en los DTOs). Hoy la validación vive a mano en los servicios; cambiarlo es una decisión de arquitectura, no un fix.

## Pruebas

xUnit v3 + NSubstitute, con las aserciones nativas de `Assert` (los DTOs de respuesta son `record`, así que `Assert.Equal` compara estructuralmente). Un proyecto de pruebas por proyecto bajo prueba, en `tests/`, con archivos que espejan el layout de `src/`. Los nombres de prueba van en **inglés**, como el resto del código: `Method_Scenario_ExpectedResult`. Las pruebas actuales de `ProductServiceTests` todavía están en español; renómbralas cuando toques el archivo.

Un cambio se considera listo cuando tiene **pruebas unitarias de la capa Application y pruebas de integración** para repositorios y controllers. Hoy solo hay unitarias de `ProductService`; la integración (`WebApplicationFactory`, SQLite in-memory) aún no está montada y `Program.cs` no expone `public partial class Program`, que haría falta. Ver [`docs/dotnet.md`](./docs/dotnet.md) §8 y §13.

## Estilo de código

**No hay linter ni formateador configurado**: sin `.editorconfig`, sin analizadores Roslyn, sin `TreatWarningsAsErrors`. El estilo es convención, no verificación — imítalo del archivo que estés tocando. Lo más visible: **indentación de 2 espacios**, `Nullable` e `ImplicitUsings` habilitados, y properties auto-implementadas con constructores explícitos. El repo **no** usa idioms de C# 14 (`field`, bloques `extension`, `?.=`); no los introduzcas.

## Seguridad

- No commitees `.env` ni archivos con credenciales. Hoy no hay secretos en el repo: la única cadena de conexión es un archivo SQLite local, ignorado por Git.
- En producción los secretos vienen de **variables de entorno** (`ConnectionStrings__DefaultConnection`), no de `appsettings.json`.
- **`Cors:AllowedOrigins` vacío hace que la API caiga en `AllowAnyOrigin()`.** Cómodo en local; al desplegar hay que poblarlo.
- No registres secretos, tokens ni información personal en logs.
- Asume que cualquier cosa en este repo es legible por un agente de IA — nunca pegues secretos aquí.
