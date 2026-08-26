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

<!-- TODO: listar los ADRs a medida que se crean. -->

- `adr-0001-...` — <!-- título -->
