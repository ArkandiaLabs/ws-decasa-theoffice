# ADR-0001: .NET 10 (LTS) como target framework único

## Estado

Aceptado — 2026-08-06 (fecha de la migración inicial del repositorio).

## Contexto

TheOffice arranca desde cero como la primera pieza de un ecommerce que crecerá durante varios años:
frontend, carrito, pedidos, backoffice y autenticación vienen después. La elección del target
framework fija el piso de lenguaje, la ventana de soporte y qué versiones de EF Core y ASP.NET Core
están disponibles.

La base arquitectónica del proyecto deriva de la plantilla Clean Architecture .NET de
[ManuelZapata.co](https://manuelzapata.co/), lo que hacía de .NET la plataforma natural.

### Opciones consideradas

- **.NET 10 (LTS)** — la versión LTS vigente al arrancar. Habilita C# 14, EF Core 10 y las
  novedades de ASP.NET Core 10 (OpenAPI 3.1 por defecto en el generador integrado).
- **.NET 8 (LTS anterior)** — más rodada y con más ejemplos en circulación, pero con una ventana de
  soporte que se cierra antes y sin acceso a EF Core 10.
- **La versión STS más reciente** — acceso más rápido a novedades, a cambio de un ciclo de soporte
  corto que obligaría a actualizar el proyecto cada pocos meses.

## Decisión

Todos los proyectos de la solución fijan `<TargetFramework>net10.0</TargetFramework>`, sin
multi-targeting. Se habilita `<Nullable>enable</Nullable>` e `<ImplicitUsings>enable</ImplicitUsings>`
de forma uniforme. No se declara `<LangVersion>`, así que aplica el default del SDK (C# 14).

No se fija la versión del SDK: `global.json` existe pero solo selecciona el runner de pruebas.

## Consecuencias

### Más fácil

- Una sola versión que mantener en los seis proyectos; sin matriz de compatibilidad.
- Acceso a EF Core 10 y al generador de OpenAPI integrado, evitando dependencias de terceros
  (Swashbuckle/NSwag) para documentar la API.
- Ventana de soporte larga: el sistema puede crecer sin una migración de framework forzada a corto
  plazo.

### Más difícil

- Cualquiera que clone el repo necesita el **SDK 10 instalado**. Como `global.json` no fija la
  versión del SDK, un SDK más nuevo se usará sin aviso — no hay reproducibilidad estricta del build.
- Ecosistema más nuevo: menos ejemplos y respuestas circulando para .NET 10 que para .NET 8.
- Al no declarar `<LangVersion>`, el piso de lenguaje se mueve solo si alguien actualiza el SDK.

### Revisitar cuando

- Salga la siguiente LTS y el soporte de .NET 10 entre en su último año.
- Se necesite reproducibilidad estricta del build (entonces: fijar `sdk.version` en `global.json`).
