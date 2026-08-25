# TheOffice — Ecommerce

**TheOffice** es una tienda en línea de artículos de oficina: papelería, mobiliario, tecnología y organización.

Este repositorio alberga el sistema completo del ecommerce. Hoy contiene únicamente su **primera pieza: el backend del catálogo**; el resto de componentes —frontends, carrito, pedidos y administración— se construyen sobre esa base.

## 🧩 Componentes del sistema

| Componente | Estado | Descripción |
|---|---|---|
| **Backend del catálogo** | ✅ Disponible | API REST de productos, categorías y clientes |
| Frontend del catálogo | 🔜 Por construir | Listado y detalle de producto para el comprador |
| Carrito y pedidos | 🔜 Por construir | Checkout sobre la entidad `Customer` |
| Backoffice | 🔜 Por construir | Administración del catálogo y del inventario |
| Autenticación | 🔜 Por construir | Cuentas de comprador y permisos de escritura |

```mermaid
flowchart LR
  subgraph HOY["Disponible hoy"]
    direction TB
    API["<b>Backend del catálogo</b><br/>.NET 10"]
    DB[("SQLite")]
    API --> DB
  end

  subgraph FUTURO["Por construir"]
    direction TB
    WEB["Frontend<br/>del catálogo"]
    CART["Carrito<br/>y pedidos"]
    ADMIN["Backoffice"]
  end

  WEB -.consume.-> API
  CART -.consume.-> API
  ADMIN -.consume.-> API
```

El backend ya expone CORS configurable por origen, así que los frontends pueden consumirlo desde otro dominio sin cambios adicionales.

---

# Backend del catálogo

Lo disponible hoy en `src/`: una API REST versionada sobre .NET 10, con arquitectura limpia por capas y lista para que crezcan nuevos casos de uso encima.

## ✨ Qué incluye

- **Catálogo de productos:** listado paginado con filtro por categoría y búsqueda por texto, y detalle por SKU.
- **Categorías:** listado para armar la navegación del catálogo.
- **Clientes:** registro y consulta, base para el futuro carrito y los pedidos.
- **API REST versionada** (`/api/v1/...`) documentada con OpenAPI 3.1 y explorable con Scalar.
- **CORS configurable** por origen.
- **Base de datos SQLite** con migraciones de EF Core y datos semilla (16 productos, 4 categorías, 3 clientes).

## 🛠️ Stack

| Componente | Versión |
|---|---|
| .NET | 10 (LTS) |
| Entity Framework Core | 10.0.10 |
| Base de datos | SQLite |
| Versionado de API | Asp.Versioning 10.2.0 |
| Documentación | Microsoft.AspNetCore.OpenApi 10.0.10 + Scalar |

## 🏁 Comenzando

### Requisitos

Además del SDK de .NET 10, el repo usa tres herramientas para sus comprobaciones automáticas.
`make` **no viene con Windows**: hay que instalarlo.

| Herramienta | macOS | Windows | Linux |
|---|---|---|---|
| SDK de .NET 10 | [descarga](https://dotnet.microsoft.com/download) | [descarga](https://dotnet.microsoft.com/download) | [descarga](https://dotnet.microsoft.com/download) |
| `make` | viene con las Xcode Command Line Tools | `winget install ezwinports.make` | viene con la distribución |
| Lefthook | `brew install lefthook` | `winget install evilmartians.lefthook` | `go install github.com/evilmartians/lefthook@latest` |
| gitleaks | `brew install gitleaks` | `winget install gitleaks` | `apt install gitleaks` en Debian trixie+ / Ubuntu 25.04+; en LTS más viejas, el binario de [releases](https://github.com/gitleaks/gitleaks/releases) |
| Git Bash | viene con el sistema | **viene con [Git para Windows](https://gitforwindows.org/)** | viene con la distribución |
| Node / `npx` | `brew install node` | `winget install OpenJS.NodeJS` | el paquete de la distribución |

Las dos últimas filas son para el instrumental del agente de IA, no para compilar el proyecto.
**Git Bash importa en Windows**: los scripts de `scripts/agent-hooks/` son bash, y si Git Bash no
está, Claude Code cae a PowerShell y los hooks **dejan de hacer nada, en silencio**. Node solo
hace falta para el servidor MCP de base de datos, que se lanza con `npx`.

### Puesta en marcha

Desde la raíz del repositorio:

1. Instala los git hooks (una sola vez):
   ```bash
   make hooks
   ```
2. Ejecuta el backend:
   ```bash
   make run
   ```
3. La API queda en `http://localhost:5226` y la documentación interactiva en `http://localhost:5226/scalar`.
4. Comprueba que todo está en orden:
   ```bash
   make check
   ```

`make check` corre estilo, compilación y pruebas — es la misma comprobación que ejecuta la
integración continua. `make help` lista el resto de objetivos.

No hace falta preparar la base de datos: en `Development` la aplicación aplica las migraciones al arrancar y siembra el catálogo. El archivo `theoffice.db` se crea local y está ignorado por Git.

Para trabajar las migraciones a mano:

```bash
dotnet ef migrations add <Nombre> -p Infrastructure/TheOffice.Persistence -s Presentation/TheOffice.Api
dotnet ef database update -p Infrastructure/TheOffice.Persistence -s Presentation/TheOffice.Api
```

### Instrumental del agente de IA

El repositorio trae dos hooks y dos servidores MCP. Qué hace cada uno está en
[`AGENTS.md`](./AGENTS.md); aquí está cómo ponerlos a funcionar.

**Los hooks son de Claude Code.** Los scripts de `scripts/agent-hooks/` son shell portable, pero
el registro vive en `.claude/settings.json` y hoy no lo lee ningún otro agente: en Codex, Cursor o
Copilot **no corren**. Tampoco son una frontera de seguridad — corren con tu shell y tus permisos,
y leen el comando, no la intención: un nombre que el comando arma en tiempo de ejecución
(`cat "$SECRET_FILE"`) les pasa por debajo.

El guardia de secretos distingue **un archivo que se abre de un nombre que solo se menciona**.
Bloquea `cat .env`, y deja pasar un `echo`, un mensaje de commit o un heredoc que hablen del
archivo. Si te bloquea algo que no abre nada, es un bug: repórtalo, no lo esquives.
Esa distinción la fija un suite de 42 casos: `make hooks-test`, incluido en `make check`.

**Variables de entorno.** Ninguna se versiona; el archivo `.mcp.json` solo guarda su nombre.

| Variable | Qué es | De dónde sale |
|---|---|---|
| `APP_DSN` | Cadena de conexión que lee el servidor MCP de base de datos | La ruta **absoluta** al SQLite local. SQLite rechaza una ruta relativa, y una ruta absoluta es específica de tu máquina — por eso no puede ir en el archivo compartido |

```bash
export APP_DSN="sqlite://$(pwd)/src/Presentation/TheOffice.Api/theoffice.db"
```

**Activar los servidores MCP.** Escribir `.mcp.json` no los conecta: quedan en
`⏸ Pending approval` hasta que confíes en el espacio de trabajo, y un repositorio recién clonado
**no puede aprobar sus propios servidores**. Ejecuta `claude` en la raíz del repo, acepta el
diálogo de confianza, y comprueba con `/mcp`.

Si tu editor subraya `${APP_DSN}` en `.mcp.json` con `Variable APP_DSN not found`, es un falso
positivo: VS Code lo lee como una variable suya. El archivo está bien y el aviso desaparece cuando
exportas la variable.

### Pruébalo

Nada de esto se ve hasta que hace algo, y un hook que nunca dispara se parece demasiado a uno que
funciona en silencio. Una línea por cada cosa instalada:

| Instalado | Pide al agente / ejecuta | Deberías ver |
|---|---|---|
| Guardia de secretos | «lee `.env`» (créalo antes) | Un rechazo que nombra los archivos de credenciales y apunta al `.example` |
| …y que no se pase de listo | «haz `echo "nota sobre .env"`» | Que corra. Mencionar el nombre no es abrirlo |
| El suite que fija esa línea | `make hooks-test` | 42/42 |
| Formateo automático | Pide agregar un método a un `.cs` con indentación descuidada | El archivo vuelve formateado y `make lint` sigue pasando |
| Servidores MCP | `/mcp` | Conectados, no `⏸ Pending approval` |
| `mslearn` | «¿qué hace `dotnet list package --vulnerable`, según la documentación oficial?» | Una respuesta que cita learn.microsoft.com |
| `dbhub` | «¿cuántas filas tiene cada tabla?» | Conteos reales de `theoffice.db` (requiere `APP_DSN`) |

## 📡 Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/v1/products` | Listado paginado del catálogo |
| `GET` | `/api/v1/products/{publicId}` | Detalle de un producto por SKU |
| `POST` | `/api/v1/products` | Crea un producto |
| `GET` | `/api/v1/categories` | Listado de categorías |
| `GET` | `/api/v1/customers/{publicId}` | Detalle de un cliente |
| `POST` | `/api/v1/customers` | Crea un cliente |

Parámetros de `GET /api/v1/products`:

| Parámetro | Default | Descripción |
|---|---|---|
| `page` | `1` | Página solicitada |
| `pageSize` | `6` | Elementos por página (máximo 50) |
| `category` | — | Filtra por `slug` de categoría |
| `search` | — | Busca en nombre y descripción |

La respuesta del listado trae `items`, `page`, `pageSize`, `totalItems` y `totalPages`. Solo se devuelven productos con `IsActive = true`.

En `src/Presentation/TheOffice.Api/TheOffice.Api.http` hay una petición lista para cada endpoint.

## 🧱 Arquitectura

```mermaid
flowchart TD
  API["<b>Presentation</b><br/>TheOffice.Api<br/><i>controllers, CORS, OpenAPI</i>"]
  APP["<b>Application</b><br/>TheOffice.Application<br/><i>servicios, DTOs, interfaces</i>"]
  PER["<b>Infrastructure</b><br/>TheOffice.Persistence<br/><i>EF Core, repositorios</i>"]
  ADA["<b>Infrastructure</b><br/>TheOffice.Adapters<br/><i>notificaciones, servicios externos</i>"]
  DOM["<b>Domain</b><br/>TheOffice.Domain<br/><i>entidades, enums, Result</i>"]

  API --> APP
  API --> PER
  API --> ADA
  PER --> APP
  ADA --> APP
  APP --> DOM
```

Las dependencias apuntan hacia adentro: **Domain** no conoce a nadie, **Application** define las interfaces (`IProductRepository`, `INotificationAdapter`) y la **Infrastructure** las implementa. **Presentation** solo orquesta.

### Estructura de carpetas

```
src/
├── Domain/TheOffice.Domain/               Entidades, enums, Result Pattern
├── Application/TheOffice.Application/     Servicios, DTOs, mappers, interfaces
├── Infrastructure/
│   ├── TheOffice.Persistence/             DbContext, modelos, repositorios, migraciones, seeders
│   └── TheOffice.Adapters/                Adaptadores hacia servicios externos
└── Presentation/TheOffice.Api/            Controllers y configuración de la API
```

## 💾 Modelo de datos

```mermaid
erDiagram
  CATEGORIES ||--o{ PRODUCTS : "agrupa"

  CATEGORIES {
    guid   Id          PK
    string PublicId    UK
    string Name
    string Slug        UK
    string Description
  }

  PRODUCTS {
    guid    Id         PK
    string  PublicId   UK "SKU"
    string  Name
    string  Description
    decimal Price
    string  ImageUrl
    int     Stock
    bool    IsActive
    guid    CategoryId FK
  }

  CUSTOMERS {
    guid   Id       PK
    string PublicId UK
    string Name
    string Email
    enum   Source   "Website, Email, Phone, SocialMedia"
  }
```

Todas las tablas usan un `Id` (UUID) privado y un `PublicId` para las referencias externas, según la decisión 2 de la siguiente sección.

> **Nota sobre `Price` y SQLite:** SQLite guarda `decimal` como TEXT y no soporta comparación ni ordenamiento en SQL, así que ordenar o filtrar por precio fallaría. `Price` se configura con `HasConversion<double>()` en `TheOfficeDbContext`. Es el workaround estándar de EF Core para SQLite y se retira al migrar a SQL Server.

## 🧠 Decisiones de implementación

1. Definimos [DTOs (Data Transfer Objects)](https://www.youtube.com/watch?v=4p6z6hL8BNg) en la capa de **Aplicación** para su uso entre la capa de **Infraestructura** y la capa de **Aplicación**. Esta estrategia nos permite contar con los DTOs necesarios en diferentes capas, facilitando no tener que hacer un mapeo 1:1. Por ejemplo, evitamos tener distintos DTOs para los contratos del **Web API** y la capa de **Aplicación** solo para transferir la información de las solicitudes a los Servicios.

2. Usamos **Public IDs** para las entidades expuestas a consumidores externos. Los IDs que viajan en las solicitudes de productos, categorías o clientes son IDs públicos (`PRD-001`, `CAT-001`, `CUS-001`). Aunque internamente implica un paso adicional para resolver el ID de base de datos, preferimos este enfoque por:

    * **Seguridad:** Los IDs de la base de datos suelen ser secuenciales y predecibles. Exponerlos podría revelar el volumen de los datos o facilitar inferir IDs válidos.
    * **Flexibilidad:** Permiten cambiar la estructura interna de la base de datos sin afectar a los consumidores de la API.
    * **Ofuscación:** Evitan dar una visión directa de la estructura y los patrones de crecimiento de la base de datos.

3. Usamos el **Result Pattern (RP)**, con nuestra propia implementación, para gestionar reglas de negocio, validaciones y operaciones de acceso a datos.

    Ventajas:

    * **Separación de responsabilidades:** Maneja de forma explícita los resultados, separando éxito y error.
    * **Estandarización en el manejo de errores:** En lugar de lanzar excepciones para el control de flujo, devuelve un objeto con la información del error.
    * **Claridad y simplicidad:** Hace explícito si una operación fue exitosa o fallida.
    * **Facilita testing y mantenimiento:** No hay que capturar excepciones inesperadas, se prueban las respuestas esperadas.

    Desventajas: sobrecarga de código, complejidad adicional, adopción en todo el equipo y pérdida del stack trace.

    Alternativas en .NET: [ErrorOr](https://github.com/amantinband/error-or) y [FluentResults](https://github.com/altmann/FluentResults).

4. No agregamos **Async** al nombre de los métodos asíncronos. Es una convención que ya no aporta claridad; la decisión puede variar según tus estándares.

5. Los objetos del **Dominio** no mapean uno a uno con los objetos de persistencia. El dominio contiene únicamente lo necesario para satisfacer las demandas del negocio; la traducción vive en los mappers de cada capa.

---

## 🗺️ Roadmap

Próximos pasos del sistema, en orden aproximado:

1. Frontend del catálogo: listado y detalle de producto.
2. Imágenes múltiples y variantes de producto.
3. Carrito de compras y pedidos.
4. Autenticación y autorización para las operaciones de escritura.
5. Backoffice de administración del catálogo.
6. Validaciones de entrada y manejo centralizado de errores.
7. Pruebas unitarias y de integración.
8. Migración a SQL Server.

## 📄 Licencia

Distribuido bajo licencia MIT. Consulta [license.md](license.md).

La base arquitectónica de este proyecto deriva de la plantilla Clean Architecture .NET de [ManuelZapata.co](https://manuelzapata.co/), también MIT.

© 2026 Arkandia.
