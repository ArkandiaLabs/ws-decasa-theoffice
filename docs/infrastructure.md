# Infraestructura — TheOffice

> Estado: **solo desarrollo local.** No hay CI, ni contenedor, ni IaC, ni entorno productivo.
> Todo lo que dice "producción" acá es intención, no algo que exista. No asumas lo contrario.

## Desarrollo local

### Prerrequisitos

- **.NET SDK 10.** `global.json` **sí** fija la versión: `10.0.100` con
  `rollForward: latestFeature`, así que cualquier SDK 10.0.x igual o superior sirve, pero uno de
  .NET 9 u 11 no. También selecciona el runner de pruebas (Microsoft.Testing.Platform).
- **`make`, Lefthook y gitleaks**, para las comprobaciones automáticas. Los comandos de
  instalación por sistema operativo están en el [`README.md`](../README.md). `make` no viene con
  Windows.
- **`dotnet-ef` como herramienta global**, únicamente si vas a trabajar migraciones a mano:
  ```bash
  dotnet tool install --global dotnet-ef
  ```
  No hay `.config/dotnet-tools.json`, así que `dotnet tool restore` no aplica.
- Nada más: **no hace falta motor de base de datos ni Docker**. SQLite es un archivo local.

### Inicio rápido

```bash
cd src
dotnet restore
dotnet run --project Presentation/TheOffice.Api
```

En `Development` la app aplica las migraciones y siembra el catálogo al arrancar. El archivo
`theoffice.db` se crea local y está en `.gitignore`. Para empezar de cero, bórralo y vuelve a
ejecutar.

### Servicios (local)

| Servicio | URL | Propósito |
|---|---|---|
| API REST | `http://localhost:5226` | Endpoints del catálogo, bajo `/api/v1/...` |
| Documentación (Scalar) | `http://localhost:5226/scalar` | UI interactiva. **Solo se monta en `Development`.** |
| Documento OpenAPI | `http://localhost:5226/openapi/v1.json` | OpenAPI 3.1, un documento por versión de API. Solo en `Development`. |
| SQLite | `src/Presentation/TheOffice.Api/theoffice.db` | Archivo local, ignorado por Git |

El perfil `https` de `launchSettings.json` también levanta `https://localhost:7118`.
`src/Presentation/TheOffice.Api/TheOffice.Api.http` tiene una petición lista para cada endpoint.

### Variables de entorno

No hay `.env` ni `.env.example` en este repo, y **no hay secretos hoy**: la única cadena de
conexión es `Data Source=theoffice.db`, un archivo local.

La configuración se lee de `appsettings.json` → `appsettings.Development.json` → variables de
entorno. Para sobreescribir sin tocar archivos, usa la convención de doble guion bajo:

```bash
ConnectionStrings__DefaultConnection="..."
Cors__AllowedOrigins__0="https://tienda.ejemplo.com"
```

**`Cors:AllowedOrigins` está vacío en ambos `appsettings`.** Cuando está vacío, `Program.cs` cae en
`AllowAnyOrigin()`. Es cómodo en local, pero desplegar así deja la API abierta a cualquier origen:
al desplegar, poblá el arreglo.

## Producción

### Objetivo de despliegue

**Aún sin definir.** No hay `Dockerfile`, `compose*.yml`, Terraform, Bicep, ni propiedades de
publicación de contenedor del SDK en ningún `.csproj`.

Cuando se defina, dos cosas que ya sabemos que habrá que resolver:

- **Las migraciones no se aplican solas fuera de `Development`.** `context.Database.Migrate()` está
  dentro de un `if (app.Environment.IsDevelopment())`. Producción necesita aplicarlas por fuera
  (`dotnet ef database update`, un migration bundle, o un paso de despliegue).
- **SQLite es un archivo local.** No sobrevive a un contenedor efímero ni a más de una instancia.
  El punto 8 del roadmap es migrar a SQL Server; ver
  [ADR-0002](./adrs/adr-0002-persistencia-sqlite-ef-core.md).

<!-- TODO: definir destino de despliegue y completar la topología. -->

### Topología

<!-- TODO: no existe todavía. Documentar cuando se defina el destino de despliegue. -->

### Secretos

En producción los secretos vendrán de **variables de entorno** inyectadas por el host —
`appsettings.json` solo conserva los defaults de desarrollo. No hay Key Vault ni `user-secrets`
configurados (`<UserSecretsId>` no está declarado en ningún proyecto).

### CI/CD

- **Herramienta:** GitHub Actions. El workflow vive en `.github/workflows/ci.yml`.
- **Alcance:** solo compuertas de calidad. **No despliega** — no hay destino de despliegue
  definido todavía (ver [objetivo de despliegue](#objetivo-de-despliegue)).
- **Disparador:** cada push de cada rama, más los pull requests desde forks. Un segundo push a la
  misma rama cancela el anterior en vez de encolarse. Un PR interno no dispara el pipeline dos
  veces.
- **Runner:** `ubuntu-latest`, que ya trae `make`.
- **Pasos, en orden:**
  1. `checkout` con historia completa — gitleaks escanea commits, no solo el árbol.
  2. Instalar gitleaks desde su release, en una versión fijada y verificada contra el
     `checksums.txt` publicado. Se sube como cualquier otra dependencia, no se resuelve sola en
     cada corrida.
  3. Instalar el SDK **desde `global.json`**, no desde una versión escrita en el workflow.
  4. Caché de paquetes NuGet, con clave sobre los `packages.lock.json`.
  5. `make ci` — restauración en modo bloqueado, estilo, compilación, pruebas y escaneo de
     secretos. El workflow **no repite los pasos**: si alguien agrega una comprobación al
     `Makefile`, CI la hereda.
  6. Publicar los `.trx` de las pruebas como artefacto.
- **Lo que falta para que sea una compuerta:** el pipeline hoy **reporta**, no bloquea. Hay que
  exigir el status check `check` en la protección de rama de `main`
  (Settings → Branches). Mientras tanto, un PR en rojo se puede mergear.
- **Asimetría local/CI:** `make check` (local, y lo que corre el hook de `pre-push`) no incluye el
  escaneo de secretos ni la restauración en modo bloqueado; `make ci` sí. En sentido contrario, el
  escaneo de secretos de cada commit lo hace el hook de `pre-commit` con `gitleaks protect`, que en
  CI no existe porque ahí ya es historia.

<!-- TODO: documentar el pipeline de despliegue cuando exista, incluyendo dónde se aplican las migraciones. -->

## Observabilidad

**TODO — aún no configurado.** No hay OpenTelemetry, health checks, Serilog ni NLog. Lo único que
existe es el logging por defecto de `Microsoft.Extensions.Logging`, con los niveles definidos en
`appsettings.json` (`Default: Information`, `Microsoft.AspNetCore: Warning`). No asumas que hay
un endpoint de health, trazas o métricas.

## Docs relacionados

- [Arquitectura](./architecture.md) · [Guía de la solución .NET](./dotnet.md) · [Decisiones](./adrs/)
