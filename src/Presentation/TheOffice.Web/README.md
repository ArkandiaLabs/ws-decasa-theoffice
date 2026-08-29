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

| Comando        | Qué hace                                                                |
| -------------- | ----------------------------------------------------------------------- |
| `pnpm start`   | Servidor de desarrollo con el proxy enganchado                          |
| `pnpm build`   | Compilación de producción a `dist/`                                     |
| `pnpm test:ci` | Pruebas, una corrida, sin navegador (Vitest + jsdom)                    |
| `pnpm lint`    | ESLint + Prettier en modo verificación                                  |
| `pnpm format`  | Corrige el formato en el sitio, incluido el orden de clases de Tailwind |

Desde la raíz del repo los mismos objetivos existen con prefijo `web-` (`make web-lint`,
`make web-build`, `make web-test`), y `make check` los corre todos. **Esa es la señal de que el
trabajo está bien, no «compila en mi carpeta».**

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

Sin Angular Material, sin PrimeNG, sin CSS-in-JS. La identidad visual viene del canvas de diseño, y
un framework de componentes con su propio lenguaje visual se pelea con ella en cada control.

El costo de esa decisión es que **la accesibilidad de teclado, los roles ARIA y el manejo de foco
son nuestros**. El foco visible es global, en `styles.css`; no se redefine por componente.

Tailwind v4 no usa `tailwind.config.js`: la configuración es el bloque `@theme` de
[`src/styles.css`](./src/styles.css), y el build lo procesa vía
[`.postcssrc.json`](./.postcssrc.json).

### Mapeo de los tokens del diseño

Los nombres del canvas son los nombres de las clases. **Cero valores arbitrarios**
(`bg-[#f4f4f5]`): si falta un color, falta un token — se agrega al `@theme`, no a la plantilla.

| Token del diseño           | Variable en `@theme`                        | Clases que genera                                          |
| -------------------------- | ------------------------------------------- | ---------------------------------------------------------- |
| `primary-900` `#10243D`    | `--color-primary-900`                       | `bg-primary-900`, `text-primary-900`, `border-primary-900` |
| `primary-700` `#1B3A61`    | `--color-primary-700`                       | botones primarios, enlaces                                 |
| `primary-500` `#2A5A94`    | `--color-primary-500`                       | foco, hover                                                |
| `primary-100` `#E3EBF4`    | `--color-primary-100`                       | chip de categoría activo                                   |
| `accent-600` `#C2761A`     | `--color-accent-600`                        | acentos de marca                                           |
| `accent-100` `#F6E7D2`     | `--color-accent-100`                        | avisos suaves                                              |
| `surface` `#FFFFFF`        | `--color-surface`                           | `bg-surface`                                               |
| `surface-muted` `#F6F7F9`  | `--color-surface-muted`                     | fondo de página, botón secundario                          |
| `skeleton` `#E9ECF1`       | `--color-skeleton`                          | skeletons y marcador «sin imagen»                          |
| `border` / `border-strong` | `--color-border` / `--color-border-strong`  | `border-border`, `border-border-strong`                    |
| `text` `#16202B`           | `--color-text`                              | **`text-text`** (el token se llama `text`)                 |
| `text-body` `#3C4A59`      | `--color-text-body`                         | `text-text-body`                                           |
| `text-muted` `#5A6672`     | `--color-text-muted`                        | `text-text-muted`                                          |
| `text-disabled` `#98A2AE`  | `--color-text-disabled`                     | botón deshabilitado — **no se usa opacidad**               |
| `text-on-primary-muted`    | `--color-text-on-primary-muted`             | texto sobre el header oscuro                               |
| Estados de stock           | `--color-stock-{ok,low,out}-{bg,fg,border}` | `bg-stock-ok-bg`, `text-stock-ok-fg`, …                    |
| Descontinuado              | `--color-discontinued-{bg,fg,border}`       | solo en el detalle                                         |

Tipografía — self-hosted con `@fontsource`, importada desde `styles.css`. Nada de CDN: la identidad
de marca no debe colgar de la red del usuario.

| Familia       | Variable         | Clase          | Uso               |
| ------------- | ---------------- | -------------- | ----------------- |
| Archivo       | `--font-display` | `font-display` | títulos y precios |
| IBM Plex Sans | `--font-sans`    | `font-sans`    | cuerpo y UI       |
| IBM Plex Mono | `--font-mono`    | `font-mono`    | SKU y datos       |

Escala de texto — cada tamaño trae ya su interlineado y su grosor:
`text-display` (40/700) · `text-h1` (30/700) · `text-price` (24/700) · `text-h3-card` (18/600) ·
`text-body` (16/400) · `text-ui` (14/500) · `text-mono-sku` (13/600) · `text-caption` (12/400).

Radios `rounded-sm|md|lg|xl` (2/4/6/8 px) y sombras `shadow-sm` / `shadow-md`. La escala de
espaciado base-4 del diseño ya es la de Tailwind; no hace falta redefinirla.

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
