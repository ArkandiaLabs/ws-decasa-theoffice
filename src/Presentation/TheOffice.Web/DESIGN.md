---
version: alpha
name: Arkandia
description: Calidez ambar, precision azul, disciplina plana.
colors:
  primary: '#FBB03B'
  primary-hover: '#E89A2A'
  primary-active: '#D08819'
  primary-disabled: '#FBD68B'
  primary-strong: '#875910'
  secondary: '#78716C'
  tertiary: '#3B86FB'
  tertiary-strong: '#0559DD'
  neutral: '#FEFAF6'
  surface: '#F1E9DA'
  surface-raised: '#FFFFFF'
  foreground: '#000000'
  on-primary: '#000000'
  on-secondary: '#FFFFFF'
  on-tertiary: '#FFFFFF'
  destructive: '#991B1B'
  on-destructive: '#FFFFFF'
  focus-ring: '#3B86FB'
  border: '#E5E0D6'
  border-strong: '#C9C2B5'
  text-muted: '#5C544D'
  text-faint: '#8A827A'
typography:
  h1:
    fontFamily: Aleo
    fontSize: 2.25rem
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: -0.02em
  h2:
    fontFamily: Aleo
    fontSize: 1.5rem
    fontWeight: 700
    lineHeight: 1.25
  h3:
    fontFamily: Aleo
    fontSize: 1.25rem
    fontWeight: 600
    lineHeight: 1.3
  body:
    fontFamily: Rubik
    fontSize: 1rem
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: Rubik
    fontSize: 0.875rem
    fontWeight: 500
    lineHeight: 1.4
  caption:
    fontFamily: Rubik
    fontSize: 0.8125rem
    fontWeight: 400
  code:
    fontFamily: Fira Code
    fontSize: 0.875rem
    fontWeight: 400
    lineHeight: 1.5
spacing:
  xs: 4px
  sm: 8px
  md: 16px
  lg: 32px
  xl: 64px
rounded:
  sm: 3px
  md: 6px
  lg: 12px
---

# Arkandia — Sistema de diseño

Este archivo es la **fuente de verdad** de la UI de `TheOffice.Web`. Los tokens son los del
**Arkandia Design System** (el proyecto de Claude Design del mismo nombre, cuya fuente canónica es
`campus-prep/frontend/DESIGN.md`): calidez ámbar, precisión azul, disciplina plana.

El frontmatter YAML de arriba son los tokens normativos; la prosa de abajo explica cómo aplicarlos.
Las plantillas consumen las clases que Tailwind deriva de ellos (`bg-surface`, `text-h1`,
`rounded-md`) — **nunca un valor crudo**.

De este archivo salen dos artefactos **generados, que no se editan a mano**:

```text
DESIGN.md ──┬── design.md export --format dtcg ──▶ design/tokens.json   (interoperabilidad)
            └── scripts/generate-theme.mjs ──────▶ src/theme.css        (@theme de Tailwind)
```

Después de editar este archivo corre `make web-tokens` y commitea los tres;
`make web-design-check` falla si divergen.

## Overview

Tono **directo y práctico, anti-hype**. La interfaz es plana a propósito: sin degradados, sin
sombras decorativas, sin animaciones de escala. La separación se logra con **filete y tonalidad**,
no con profundidad.

La densidad de información es **moderada** — ni apretada ni de whitespace de marketing. Quien usa
esto está comprando para una empresa y viene a verificar una referencia, un precio y una
existencia. Sentence case siempre, nunca Title Case. Sin emoji: el único carácter admitido es `✓`.

No hay tema oscuro, y no está pendiente: es un [hueco deliberado](../../../AGENTS.md).

## Colors

Tonos tierra cálidos con **un solo acento frío**. Hay exactamente un color primario (ámbar
`#FBB03B`) y exactamente un acento (zafiro `#3B86FB`). **Nunca se mezclan en el mismo elemento.**

- **Primary (#FBB03B):** El oro de marca. Conduce las acciones primarias —`primary-hover` /
  `primary-active` lo oscurecen al pasar y al presionar; `primary-disabled` es su versión inerte— y
  **pinta la banda del encabezado**, que es la firma de marca de la pantalla. Encima siempre va
  `on-primary` (negro), que da 11.38:1.

  Que el encabezado sea ámbar es una **decisión de este producto**, no del sistema de origen: allí
  el ámbar es solo de acción y la regla es «una sola CTA ámbar por pantalla». Aquí la banda no es
  accionable, así que la regla se mantiene para los **controles**: sigue habiendo una sola acción
  ámbar por vista. Si algún día compiten, gana el botón y el encabezado cambia.

  Sobre el ámbar, el anillo de foco usa `tertiary-strong`: el zafiro de marca da 1.90:1 encima, bajo
  el mínimo de 3:1. Es la única excepción al foco global, y vive en `styles.css`, no por componente.

- **On-primary (#000000):** Sobre el ámbar va **tinta negra**, no blanco. El blanco sobre este oro
  no alcanza el contraste AA.
- **Secondary (#78716C):** Una piedra cálida para el **filete del control** enfocable y los rellenos
  donde encima va `on-secondary` (blanco). **No para texto sobre crema**: ahí da 3.98:1. El texto
  atenuado usa `text-muted` (#5C544D), que da 6.15:1.
- **Tertiary (#3B86FB):** Zafiro para el anillo de foco y algún icono de acento. **Nunca para
  acciones primarias.** Encima va `on-tertiary` (blanco), pero ese par da **3.51:1**: sirve para un
  icono o un borde, **no para texto**. Si hace falta un relleno con texto blanco encima, el fondo es
  `tertiary-strong`. Es un hueco de la paleta de origen, no de este repo.
- **Tertiary-strong (#0559DD) / Primary-strong (#875910):** Los mismos dos tonos, oscurecidos hasta
  ser legibles **como texto**. Son una adición: sobre crema, el zafiro da 2.91:1 y el ámbar
  `primary-active` 2.41:1, muy por debajo del 4.5:1 de AA. El zafiro de marca se sigue usando como
  relleno y como anillo de foco, donde el mínimo es 3:1; el texto usa la variante fuerte. Sigue la
  convención de nombres del propio sistema (`border` / `border-strong`), y vale la pena subirlo al
  proyecto de diseño.
- **Neutral (#FEFAF6):** Pergamino — el fondo de la página. **Nunca blanco puro**: la calidez
  sostiene la identidad.
- **Surface (#F1E9DA):** Crema, un punto más profundo que la página, para tarjetas y regiones
  agrupadas.
- **Surface-raised (#FFFFFF):** Blanco, para el encabezado y los controles de entrada. **Es una
  adición a la paleta de origen:** su `colors_and_type.css` no nombra el blanco, pero su navbar y
  su input lo pintan (`background:#fff`). Vale la pena subirlo al proyecto de diseño.
- **Foreground (#000000):** Tinta pura para el texto principal, no gris. El texto atenuado usa
  `text-muted` (#5C544D) y el de apoyo `text-faint` (#8A827A).
- **Border (#E5E0D6) / Border-strong (#C9C2B5):** Filetes cálidos. Las tarjetas se separan con
  borde y radio, **no con sombra**.
- **Destructive (#991B1B):** Carmesí profundo, reservado a errores y acciones irreversibles.
- **Focus-ring (#3B86FB):** Zafiro, 2px, siempre visible. **Nunca se suprime.**

Los **estados de inventario** del catálogo no tienen color propio en este sistema: se expresan con
un chip `neutral` con filete, y lo que cambia es la tinta y el borde. **Disponible es el estado por
defecto y por eso es el más callado**: va en `text-muted`, no en tinta negra — el color se reserva
para la excepción. Existencias bajas va en `primary-strong` con filete `primary`, agotado en
`destructive`, y descontinuado en `text-muted` con filete `border-strong`. **El color nunca comunica solo:** siempre
acompaña a un símbolo y a una palabra.

## Typography

Tres familias, no hay una cuarta. Van **self-hosted** con `@fontsource` en vez del CDN de Google
Fonts que usa el proyecto de diseño: la identidad de marca no debe colgar de la red del usuario.
Solo se empaquetan los pesos que la escala usa.

- **Aleo** (slab serif, `font-display`) — solo `h1`, `h2` y `h3`. Peso editorial.
- **Rubik** (sans humanista, `font-body`) — cuerpo, etiquetas y toda la prosa de interfaz.
- **Fira Code** (monoespaciada, `font-mono`) — solo código e identificadores, como el `PublicId`.

Nunca Aleo para texto corrido ni Rubik para un titular. Cada nivel trae ya su interlineado y su
grosor, así que `font-display text-h1` resuelve el bloque completo.

## Layout

Contenedores: `5xl` (1024px) para vistas de listado, `3xl` (768px) para lectura. Los componentes
deben ser fluidos entre 320px y 1920px, y la pantalla tiene que **sobrevivir a 360px de ancho y al
zoom al 200%** sin desbordarse: los nombres largos rompen palabra, no la caja.

Rejilla estricta de 8px con medio paso de 4px (`xs 4 / sm 8 / md 16 / lg 32 / xl 64`), sin valores
fuera de la rejilla. Todos esos valores existen en la escala numérica de Tailwind (`p-4` = `md`,
`p-8` = `lg`, `p-16` = `xl`), que es la forma que usan las plantillas: el generador **no** emite
`--spacing-*` porque declararlos ensombrece la escala de contenedores y dejaría `max-w-xl` en 64px
sin que nada lo reporte.

Objetivo táctil mínimo de 44×44px.

## Elevation & Depth

**Plano por diseño.** Sin sombras en reposo: la separación es borde + tonalidad. El sistema de
origen reserva una sombra mínima para el hover de una tarjeta y otra para superposiciones; ninguna
se usa en estas dos pantallas, y el formato DESIGN.md no tiene tokens de elevación.

Sin degradados, sin `backdrop-filter`, sin glassmorphism, sin imágenes de fondo.

## Shapes

Casi plano, angular y deliberado: `sm` (3px) por defecto para botones e inputs, `md` (6px) para
tarjetas y `lg` (12px) solo para contenedores grandes. **Sin píldoras**, sin redondeos excesivos.

## Do's and Don'ts

- Do usar `primary` (ámbar) para exactamente una acción por pantalla, con `on-primary` (negro).
- Do usar `tertiary` (zafiro) para enlaces y el anillo de foco — **nunca para una acción primaria**.
- Don't poner ámbar y zafiro dentro del mismo componente.
- Do consumir las clases derivadas de los tokens (`bg-surface`, `text-label`, `rounded-md`). Si
  falta un color, **falta un token en este archivo** — se agrega aquí y se regenera.
- Don't escribir valores arbitrarios de Tailwind (`bg-[#f4f4f5]`, `text-[13px]`) en las plantillas.
- Don't introducir degradados ni sombras decorativas: el sistema es plano a propósito.
- Don't usar blanco puro como fondo de página; el pergamino es carga de marca.
- Don't apagar un control con opacidad: aquí se apaga con color y cursor, para no bajar del
  contraste AA. Es una de las dos desviaciones deliberadas del sistema de origen, que usa
  `opacity-50`.
- Do usar `secondary` como filete del control que se puede enfocar (input, botón secundario,
  paginador). Es la otra desviación: el sistema de origen usa `border` (1px `#E5E0D6`), que sobre
  pergamino da 1.15:1 y no llega al 3:1 que pide WCAG 1.4.11 para el borde que identifica un
  control.
- Don't comunicar un estado solo con color: siempre acompañado de símbolo y palabra.
- Do mantener el contraste WCAG AA (4.5:1 en texto, 3:1 en el borde que identifica un control).
- Don't usar `text-faint` (#8A827A) ni `secondary` como color de texto: dan 3.13:1 y 3.98:1 sobre
  crema. Para texto atenuado, `text-muted`.
- **Ojo con el nombre de la clase:** los tokens `text-muted` y `text-faint` generan
  `text-text-muted` y `text-text-faint`. `text-muted` a secas cae en el namespace de tamaños de
  Tailwind, no genera nada y el elemento hereda negro **sin que nada lo reporte**.
