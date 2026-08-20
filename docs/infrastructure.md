# Infraestructura — TheOffice

> Estado: **solo desarrollo local.** No hay CI, ni contenedor, ni IaC, ni entorno productivo.
> Todo lo que dice "producción" acá es intención, no algo que exista. No asumas lo contrario.

## Desarrollo local

### Prerrequisitos

- **.NET SDK 10.** `global.json` **no** fija la versión del SDK, solo el runner de pruebas, así
  que cualquier SDK 10 instalado sirve.
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

- **Herramienta:** ninguna. No hay `.github/workflows/`, `Jenkinsfile`, `azure-pipelines.yml` ni
  `.gitlab-ci.yml`.
- **Intención:** que CI despliegue al hacer merge a `main`. **Todavía no está implementado.**
- **Hoy:** el flujo es local — `dotnet build` y `dotnet test` a mano, y PRs hacia `main` revisados
  por el equipo de Arkandia (no hay `CODEOWNERS`).

<!-- TODO: documentar el pipeline cuando exista, incluyendo dónde se aplican las migraciones. -->

## Observabilidad

**TODO — aún no configurado.** No hay OpenTelemetry, health checks, Serilog ni NLog. Lo único que
existe es el logging por defecto de `Microsoft.Extensions.Logging`, con los niveles definidos en
`appsettings.json` (`Default: Information`, `Microsoft.AspNetCore: Warning`). No asumas que hay
un endpoint de health, trazas o métricas.

## Docs relacionados

- [Arquitectura](./architecture.md) · [Guía de la solución .NET](./dotnet.md) · [Decisiones](./adrs/)
