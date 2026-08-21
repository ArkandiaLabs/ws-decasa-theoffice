# ADR-0002: SQLite + EF Core como persistencia inicial

## Estado

Aceptado, **explícitamente temporal**. El punto 8 del roadmap del `README.md` es migrar a
SQL Server.

## Contexto

El backend del catálogo tenía que ser ejecutable desde un clone limpio sin fricción: sin instalar
un motor de base de datos, sin levantar contenedores, sin configurar credenciales. El volumen de
datos es un catálogo semilla (16 productos, 4 categorías, 3 clientes) y no hay SLA ni concurrencia
real todavía.

Al mismo tiempo, el diseño debía permitir cambiar de motor sin reescribir la aplicación — de ahí
que los repositorios estén detrás de puertos definidos en `TheOffice.Application`.

### Opciones consideradas

- **SQLite con EF Core** — cero instalación, la BD es un archivo local ignorado por Git, y las
  migraciones de EF Core funcionan igual que con cualquier otro proveedor.
- **SQL Server (LocalDB o contenedor)** — el destino real a futuro, pero exige instalar el motor o
  Docker para cualquiera que quiera correr el proyecto.
- **PostgreSQL en contenedor** — igual de capaz, misma fricción de Docker, y sin la afinidad con
  el ecosistema .NET que tiene SQL Server para este equipo.
- **En memoria (`UseInMemoryDatabase`)** — la fricción más baja, pero no ejercita migraciones ni
  SQL real, así que escondería problemas hasta el momento de migrar.

## Decisión

SQLite vía `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10, con la cadena de conexión
`Data Source=theoffice.db` en `appsettings.json`. Migraciones de EF Core, aplicadas
automáticamente con `Database.Migrate()` al arrancar **solo en `Development`**. Los datos semilla
van por `HasData` dentro de `OnModelCreating`.

Se acepta un workaround específico del proveedor: **`Price` se configura con
`HasConversion<double>()`** en `TheOfficeDbContext`, porque SQLite almacena `decimal` como TEXT y
no puede compararlo ni ordenarlo en SQL — sin la conversión, filtrar u ordenar por precio fallaría.

## Consecuencias

### Más fácil

- `dotnet run` y ya: la base se crea, se migra y se siembra sola en desarrollo.
- Onboarding sin prerrequisitos más allá del SDK de .NET.
- Las pruebas de integración futuras pueden usar SQLite in-memory sin infraestructura.

### Más difícil

- **`Price` pierde precisión decimal** al viajar por `double`. Es tolerable en un catálogo; no lo
  sería en montos de pedido o facturación.
- **SQLite no sobrevive a un despliegue real**: es un archivo local, no soporta múltiples
  instancias ni contenedores efímeros. Esto bloquea el despliegue hasta que se migre.
- El workaround de `HasConversion<double>()` es una trampa latente: **debe retirarse al migrar a
  SQL Server**, y no debe copiarse a otros campos decimales mientras tanto.
- Las migraciones **no se aplican fuera de `Development`**: producción necesita un paso propio.

### Revisitar cuando

- Se defina un destino de despliegue real (ver [infraestructura](../infrastructure.md)).
- Aparezcan montos de dinero que exijan precisión decimal exacta — carrito, pedidos, facturación.
- El catálogo crezca lo suficiente para que la concurrencia de escritura importe.
