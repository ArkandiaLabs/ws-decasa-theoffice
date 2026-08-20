# Registros de Decisiones de Arquitectura (ADRs)

Este directorio contiene **ADRs** — registros cortos y fechados de decisiones arquitectónicamente significativas. Explican *por qué* el código se ve como se ve, lo cual es más difícil de inferir leyendo la fuente que *qué* hace.

## Cuándo escribir uno

Escribe un ADR cuando:
- Elijas entre dos o más opciones viables (lenguaje, framework, patrón, herramienta).
- Aceptes un trade-off que futuros contribuidores querrán revisitar.
- Adoptes una convención que no está reforzada por tooling.

No escribas uno para detalles de implementación reversibles.

## Formato

Usa `adr-template.md` como punto de partida. Cada ADR tiene cuatro secciones:

1. **Estado** — Propuesto / Aceptado / Reemplazado.
2. **Contexto** — el problema, las opciones consideradas y las restricciones.
3. **Decisión** — qué elegimos.
4. **Consecuencias** — qué se vuelve más fácil, qué se vuelve más difícil.

Mantén los ADRs cortos (1 página). Nómbralos `adr-NNNN-slug-corto.md` con un contador con ceros a la izquierda.

## Índice

- [`adr-0001-target-framework-net10.md`](./adr-0001-target-framework-net10.md) — .NET 10 (LTS) como target framework único.
- [`adr-0002-persistencia-sqlite-ef-core.md`](./adr-0002-persistencia-sqlite-ef-core.md) — SQLite + EF Core, y el workaround de `Price`.
- [`adr-0003-result-pattern.md`](./adr-0003-result-pattern.md) — Result Pattern propio en vez de excepciones.

## Candidatos a promover

Estas decisiones **ya están tomadas y documentadas** en la sección "Decisiones de implementación"
del [`README.md`](../../README.md), pero todavía no tienen ADR propio. Promoverlas es trabajo
pendiente, no una invitación a re-decidirlas:

- **DTOs en la capa Application, compartidos entre capas** (decisión 1 del README) — en vez de un
  juego de DTOs por consumidor.
- **`PublicId` para las entidades expuestas** (decisión 2) — justificación: seguridad, flexibilidad
  y ofuscación del crecimiento de la BD.
- **Sin sufijo `Async` en métodos asíncronos** (decisión 4).
- **El dominio no mapea 1:1 con persistencia** (decisión 5).
