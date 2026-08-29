# TheOffice — Frontend del catálogo

Angular del catálogo B2B. Dos pantallas de **solo lectura**: el listado (`/` y `/productos`) y la
ficha de un producto (`/productos/:publicId`, donde `publicId` es el SKU, `PRD-001`).

No hay carrito, ni pedidos, ni autenticación, ni backoffice. No es un recorte temporal: son
[huecos deliberados del roadmap](../../../AGENTS.md).

## Por qué vive aquí

`src/` se organiza por capas de arquitectura limpia, y un frontend **es** capa de presentación. No
está dentro de `TheOffice.Api/ClientApp/` porque la API **no sirve la SPA**: `Program.cs` configura
CORS por origen, es decir, los frontends viven en otro dominio.

Tampoco está en `src/TheOffice.sln`, ni tiene un `.csproj` envoltorio. Es un proyecto Node
independiente y por tanto **invisible** para `dotnet build`, `dotnet format` y las pruebas de
arquitectura. Su integración con las compuertas del repo pasa entera por el `Makefile`.

## Requisitos

Node.js **≥ 22.22.3** — la versión exacta está en [`.nvmrc`](./.nvmrc), y `nvm use` desde esta
carpeta la selecciona. El Angular CLI 22 **rechaza** versiones anteriores; no es un aviso, es un
error de arranque — por eso los objetivos `make web-*` hacen el `nvm use` ellos mismos, y no hay
que acordarse de nada al abrir una terminal nueva. Si trabajas con `pnpm` directamente desde esta
carpeta, ahí sí tienes que haber hecho `nvm use`.

**pnpm**, no npm. La versión está fijada en el campo `packageManager` de `package.json`, que es de
donde la leen tanto `corepack` como el CI. `corepack enable pnpm` basta para tenerlo, y el
`Makefile` lo hace solo si falta: pnpm se instala **por versión de Node**, así que cambiar de
versión lo deja fuera del PATH.

## Cómo correrlo

El frontend necesita el backend arriba: sin él solo puede pintar su estado de error.

```bash
make dev        # desde la raíz: API en :5226 y app en :4200, un Ctrl-C apaga los dos
```

O por separado, si prefieres dos terminales:

```bash
make run        # terminal 1, desde la raíz — API en http://localhost:5226
pnpm install    # una sola vez
pnpm start      # terminal 2 — app en http://localhost:4200
```

El navegador nunca ve el `5226`: la app pide `/api/v1` en su propio origen y
[`proxy.conf.json`](./proxy.conf.json) lo reenvía. **Cero URLs absolutas de API en el código.**

## Comandos

| Comando               | Qué hace                                                                |
| --------------------- | ----------------------------------------------------------------------- |
| `pnpm start`          | Servidor de desarrollo con el proxy enganchado                          |
| `pnpm build`          | Compilación de producción a `dist/`                                     |
| `pnpm test:ci`        | Pruebas, una corrida, sin navegador (Vitest + jsdom)                    |
| `pnpm lint`           | ESLint + Prettier en modo verificación                                  |
| `pnpm format`         | Corrige el formato en el sitio, incluido el orden de clases de Tailwind |
| `pnpm design:tokens`  | Regenera los tokens desde [`DESIGN.md`](./DESIGN.md)                    |
| `pnpm design:lint`    | Valida la estructura de `DESIGN.md`                                     |
| `pnpm design:check`   | Falla si los tokens generados divergen de `DESIGN.md`                   |
| `pnpm design:classes` | Falla si una plantilla usa una clase que los tokens no generan          |

Desde la raíz del repo los mismos objetivos existen con prefijo `web-` (`make web-lint`,
`make web-build`, `make web-test`, `make web-tokens`, `make web-design-check`), y `make check` los
corre todos. **Esa es la señal de que el trabajo está bien, no «compila en mi carpeta».**

El costo hay que decirlo: `make check` ahora paga un `pnpm install --frozen-lockfile` incluso para
un cambio de una línea en C#. Es el precio de tener una sola compuerta en vez de dos que se
desincronizan.

Dos cosas de pnpm que hay que saber antes de que muerdan, ambas explicadas en el archivo que las
arregla: [`.npmrc`](./.npmrc) eleva `@angular-eslint` a la raíz de `node_modules` porque el Angular
CLI resuelve sus builders desde ahí y no por el grafo; y `pnpm.onlyBuiltDependencies` en
`package.json` autoriza los scripts de instalación de `esbuild` y compañía, que pnpm bloquea por
defecto y sin los cuales el build no arranca.

## Pruebas

Vitest con jsdom — **sin navegador**, para que CI no dependa de tener Chrome instalado. Los nombres
siguen la convención del repo: inglés, `Method_Scenario_ExpectedResult`.

Lo que cubren las unitarias y lo que no: el `CatalogService` prueba el `404` y la API caída; los
componentes prueban los estados que **los datos semilla no producen** (`stock = 0`,
`isActive = false`, `category: null`, `imageUrl` vacío).

Lo que **no** cubren: todo lo que solo se rompe en un navegador real. El foco visible, el
desbordamiento a 360 px, el zoom al 200 %, la consola limpia. Eso se recorrió a mano con el MCP de
Chrome DevTools —queda registrado en el PR #7— y hay que repetirlo al tocar las pantallas.

## Estilos: Tailwind v4, y nada más

Sin Angular Material, sin PrimeNG, sin CSS-in-JS. La identidad visual la fija
[`DESIGN.md`](./DESIGN.md), y un framework de componentes con su propio lenguaje visual se pelea con
ella en cada control.

El costo de esa decisión es que **la accesibilidad de teclado, los roles ARIA y el manejo de foco
son nuestros**. El foco visible es global, en `styles.css`; no se redefine por componente.

Tailwind v4 no usa `tailwind.config.js`: la configuración es el bloque `@theme`, y el build lo
procesa vía [`.postcssrc.json`](./.postcssrc.json).

### De dónde salen los tokens

La fuente de verdad es [`DESIGN.md`](./DESIGN.md), y sus tokens son los del **Arkandia Design
System** — el proyecto de Claude Design del mismo nombre, cuya fuente canónica es
`campus-prep/frontend/DESIGN.md`. De ahí salen dos archivos **generados, que no se editan a mano**:

```
DESIGN.md ──┬── design.md export --format dtcg ──▶ design/tokens.json   (interoperabilidad)
            └── scripts/generate-theme.mjs ──────▶ src/theme.css        (@theme de Tailwind)
                                                          │
                                     check-classes.mjs ───┴──▶ las plantillas
```

**Las dos mitades tienen compuerta.** `design:check` cubre `DESIGN.md → CSS`; `design:classes`
cubre `CSS → plantillas`, que es por donde se colaron los dos únicos fallos reales de este sistema.
Tailwind emite toda clase que reconoce, así que «usada en la plantilla pero ausente del CSS
compilado» solo puede significar que no la reconoció — y no falla, simplemente no genera nada.
El check necesita el CSS compilado, así que corre **después** de `web-build`. Además vigila que
`theme.css` no declare `--spacing-*`, el namespace que ensombrece la escala de contenedores.

[`src/styles.css`](./src/styles.css) importa el generado y se queda con lo que no es token: las
fuentes de `@fontsource` y la capa base con el foco visible.

Tocar el sistema de diseño son tres pasos: editas `DESIGN.md`, corres `make web-tokens` y commiteas
los tres archivos. `make web-design-check` regenera y compara — **no usa git**, así que dice la
verdad en cualquier estado del árbol — y `make check` lo corre.

Tres cosas que conviene saber antes de que sorprendan:

- **El export DTCG de `@google/design.md` v0.4.0 descarta `lineHeight`.** Por eso el generador lee
  `DESIGN.md` y no `design/tokens.json`: aquí cada nivel de la escala trae su interlineado.
- **No hay tokens de elevación en el formato**, y tampoco hacen falta: el sistema es plano a
  propósito. No hay `shadow-*` y no se agregan.
- **El sistema de origen usa el CDN de Google Fonts**; aquí las tres familias van self-hosted con
  `@fontsource`, por la regla del repo de no colgar la identidad de la red del usuario.

### Mapeo de los tokens del diseño

Los nombres de los tokens son los nombres de las clases. **Cero valores arbitrarios**
(`bg-[#f4f4f5]`): si falta un color, falta un token — se agrega a `DESIGN.md`, no a la plantilla.

| Token del diseño             | Clases que genera                                           |
| ---------------------------- | ----------------------------------------------------------- |
| `primary` `#FBB03B`          | la banda del encabezado, y una acción por pantalla          |
| `primary-hover` / `-active`  | `hover:bg-primary-hover`, `active:bg-primary-active`        |
| `primary-strong` `#875910`   | el ámbar **legible como texto** (existencias bajas)         |
| `primary-disabled` `#FBD68B` | el ámbar inerte (hoy sin uso)                               |
| `on-primary` `#000000`       | `text-on-primary` — tinta negra sobre el ámbar, no blanco   |
| `secondary` `#78716C`        | `border-secondary` — el filete del control. **No es texto** |
| `tertiary` `#3B86FB`         | relleno de acento y anillo de foco                          |
| `tertiary-strong` `#0559DD`  | `text-tertiary-strong` — los enlaces                        |
| `neutral` `#FEFAF6`          | pergamino: fondo de página y chips de estado                |
| `surface` `#F1E9DA`          | crema: tarjetas y bloques agrupados                         |
| `surface-raised` `#FFFFFF`   | blanco: los inputs                                          |
| `foreground` `#000000`       | tinta del texto, y el chip de categoría activo              |
| `text-muted` `#5C544D`       | **`text-text-muted`** — todo el texto atenuado              |
| `text-faint` `#8A827A`       | solo no-texto: da 3.13:1 sobre crema                        |
| `border` / `border-strong`   | filetes cálidos; `bg-border` rellena esqueletos             |
| `destructive` `#991B1B`      | agotado y avisos de error                                   |

Los estados de inventario **no tienen tokens propios**: chip `neutral` con filete, y cambia la
tinta — `foreground` disponible, `primary-active` stock bajo, `destructive` agotado, `secondary`
descontinuado.

Tipografía — self-hosted con `@fontsource`. Cada nivel trae familia, tamaño, interlineado y grosor,
así que `font-display text-h1` resuelve el bloque completo.

| Nivel     | Familia   | Clases                   | Uso                                   |
| --------- | --------- | ------------------------ | ------------------------------------- |
| `h1`      | Aleo      | `font-display text-h1`   | título de página y nombre en la ficha |
| `h2`      | Aleo      | `font-display text-h2`   | precio, wordmark, títulos de sección  |
| `h3`      | Aleo      | `font-display text-h3`   | nombre del producto en la tarjeta     |
| `body`    | Rubik     | `font-body text-body`    | descripción y prosa larga             |
| `label`   | Rubik     | `font-body text-label`   | botones, chips, metadatos, input      |
| `caption` | Rubik     | `font-body text-caption` | texto de apoyo                        |
| `code`    | Fira Code | `font-mono text-code`    | el SKU (`PRD-001`) e identificadores  |

Radios `rounded-sm|md|lg` (3/6/12 px) — **sin píldoras**. El espaciado del diseño
(4/8/16/32/64 px) ya es la escala base-4 de Tailwind, así que se usa en forma numérica (`p-4`) y el
generador no lo emite como `--spacing-*`: hacerlo ensombrece la escala de contenedores y deja
`max-w-xl` en 64 px sin que nada lo reporte — un fallo que solo se ve en el navegador.

**Adiciones y desviaciones frente al sistema de origen**, todas anotadas en `DESIGN.md`:

| Qué                         | Por qué                                                                                       |
| --------------------------- | --------------------------------------------------------------------------------------------- |
| `surface-raised` `#FFFFFF`  | Su navbar y su input pintan `#fff`, pero su paleta no nombra el blanco (`surface` es crema).  |
| `tertiary-strong` `#0559DD` | El zafiro de marca da **2.91:1** como texto sobre crema. Mismo tono, oscurecido hasta 5.00:1. |
| `primary-strong` `#875910`  | El `primary-active` da **2.41:1** como texto. Mismo tono, 5.02:1.                             |
| Apagado sin `opacity-50`    | La opacidad atenúa también el fondo y tumba el contraste. Aquí se apaga con color y cursor.   |
| Filete `secondary`          | `border` sobre pergamino da 1.15:1, lejos del 3:1 de WCAG 1.4.11 para el borde de un control. |

Vale la pena subir las tres adiciones al proyecto de Claude Design.

**El encabezado ámbar es decisión de este producto**, no del sistema de origen, donde el ámbar es
solo de acción. La regla de «una sola ámbar por pantalla» se mantiene para los **controles**. Sobre
esa banda el anillo de foco usa `tertiary-strong`: el zafiro de marca da 1.90:1 encima, bajo el
mínimo de 3:1. Es la única excepción al foco global y vive en `styles.css`, no por componente.

**Una trampa del namespace de Tailwind:** los tokens `text-muted` y `text-faint` generan las clases
`text-text-muted` y `text-text-faint`. Escribir `text-muted` a secas **no falla**: cae en el
namespace de tamaños de fuente, no genera nada, y el elemento hereda el color del padre. Fue
exactamente así como el chip «Disponible» terminó en negro sin que build, lint ni pruebas dijeran
nada.

## Reglas no obvias

- **`pageSize` es 10 por decisión del frontend**, no el 6 que devuelve el servidor por defecto. Va
  explícito en cada petición, como constante `PAGE_SIZE` en `catalog.service.ts`.
- **La búsqueda por SKU (`^PRD-\d{3}$` → navegar al detalle) vive solo en el cliente.** La API no
  tiene ese comportamiento y no se le va a pedir.
- **Los nombres de categoría llegan sin tildes** (`Papeleria`, `Tecnologia`, `Organizacion`). Se
  renderiza lo que llega: **no se «corrige» en el frontend**.
- **Los fallos son valores, no excepciones.** `CatalogService` devuelve `Fetched<T>` con tres
  desenlaces (`ok` / `not-found` / `error`), igual que el Result pattern del backend. La pantalla
  **nunca** muestra un código HTTP.
- **`totalPages` se usa, no se recalcula**: lo calcula el servidor.
- **La descripción es texto plano.** Nunca `innerHTML`.
- `OnPush` en todo componente, `inject()` en vez de constructor, `input()`/`output()` como
  funciones, control flow `@if`/`@for`/`@empty`.
