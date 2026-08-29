# Plan de implementación — Frontend del catálogo TheOffice

**Fuente visual:** el canvas de Claude Design **“TheOffice catálogo B2B”**
(`e601ff1f-c744-4ba9-90d7-a9b65c7aa2a6`, artboards A–E). Los tokens ya están volcados en la
sección 3, así que no hace falta reabrirlo para implementar; se consulta solo ante duda de layout.

Este documento es **autosuficiente**: alcance, contrato de la API, restricciones y tokens están
adentro. Ante conflicto entre el canvas y este plan, el canvas manda sobre lo visual y este plan
sobre el alcance.

**Ubicación:** `src/Presentation/TheOffice.Web/` · **Rama:** `frontend-baseline` (la actual; no se
crean ramas nuevas) · **Entrega:** un commit convencional por fase.

---

## 1. Contexto, alcance y entregable

### Quién usa esto

TheOffice es un **ecommerce B2B de artículos de oficina** — papelería, mobiliario, tecnología y
organización. Los compradores son **departamentos de compras y secretarias** que abastecen oficinas.
La compra es recurrente, por catálogo, con volumen. **No es consumo masivo ni descubrimiento.**

El escenario real es una secretaria reponiendo papel un martes por la mañana, con prisa:
**herramienta de trabajo, no vitrina.** Densidad de información legible por encima del espectáculo.

Tres consecuencias que mandan sobre la implementación:

- **El comprador piensa en códigos.** El `publicId` (`PRD-001`) es un SKU y es su identificador de
  trabajo: visible y copiable, no letra chica.
- El patrón es **“encontrar el ítem que ya sé que necesito”**: filtro por categoría y búsqueda por
  texto. No hay feed de inspiración ni recomendados.
- **La disponibilidad importa antes de armar el pedido.** El `stock` viene en el listado, no solo en
  el detalle, precisamente por eso. Se muestra en ambos.

### Alcance: dos rutas, y solo dos

| Ruta | Pantalla |
|---|---|
| `/` y `/productos` | Listado del catálogo: grilla, filtro por categoría, búsqueda, paginación |
| `/productos/:publicId` | Detalle de un producto. `publicId` es el SKU (`PRD-001`), no un Guid ni un slug |

### Por qué el frontend vive en `src/Presentation/TheOffice.Web/`

Conviene entenderlo antes de moverlo:

1. `src/` se organiza por capas de arquitectura limpia (`Domain`, `Application`, `Infrastructure`,
   `Presentation`). Un frontend **es** capa de presentación.
2. La convención `ClientApp/` dentro del proyecto API aplica cuando **la API sirve la SPA**. Aquí no:
   `Program.cs` configura CORS por origen y el `README` declara que los frontends viven en otro
   dominio. Meterlo dentro del API contradiría esa decisión.
3. Las pruebas de arquitectura cargan *assemblies* por referencia de tipo, y `dotnet build`/`format`
   operan sobre `src/TheOffice.sln`. Una carpeta Angular fuera del `.sln` es **invisible** para esas
   herramientas — no las rompe.

### Estilos: Tailwind y nada más

**Tailwind CSS, decidido y no negociable.** Nada de Angular Material, PrimeNG ni ninguna otra
librería de componentes: la identidad visual viene del canvas, y un framework con su propio lenguaje
visual (Material Design 3 y sus tokens, formas y elevaciones) se pelea con ella en cada componente.
Tampoco CSS-in-JS.

El costo de esa decisión es que **la accesibilidad de teclado, los roles ARIA y el manejo de foco son
nuestros**. No se dejan para después: son criterio de aceptación de las fases 4, 5 y 6, y se auditan
en la 7.

### Entregable

- [ ] El proyecto Angular funcionando en `src/Presentation/TheOffice.Web/`.
- [ ] `README.md` propio en esa carpeta: instalar, correr, que necesita `make run` en la raíz, y el
      mapeo de tokens del diseño a la configuración de Tailwind.
- [ ] `.nvmrc` con la versión de Node y `package-lock.json` commiteado.
- [ ] `.gitignore` que cubra `dist/` y `.angular/` (la raíz ya ignora `node_modules/`).
- [ ] Cambios de `Makefile`, `.github/workflows/ci.yml`, `README.md` de la raíz, `AGENTS.md` y
      `lefthook.yml` descritos en las fases 1 y 8.
- [ ] Pruebas unitarias/de componente: al menos el servicio de catálogo (con `HttpTestingController`,
      cubriendo el `404` y el error de red) y los dos componentes de página.
- [ ] Los 20 escenarios de la batería de validación (fase 8) ejecutados y registrados.
- [ ] **`make check` en verde desde la raíz**, con backend y frontend dentro. Esa es la prueba de que
      el trabajo está completo — no “compila en mi carpeta”.

---

## 2. Decisiones tomadas

| Tema | Decisión | Motivo |
|---|---|---|
| Framework | Angular, versión que resuelva `npx @angular/cli@latest` | Sin fijar de memoria; adaptar idioms a esa versión |
| Estilos | Tailwind CSS **v4** (`@theme` en CSS, `@tailwindcss/postcss`) | Es la línea actual; sin `tailwind.config.js`. Si el builder de la versión instalada no la soporta, caer a v3 y **dejarlo escrito** en el README del frontend |
| Runner de pruebas | El del CLI, prefiriendo **sin navegador** (Vitest) sobre Karma | Evita depender de Chrome en el runner de CI |
| Fuentes | **Self-hosted con `@fontsource`** (Archivo, IBM Plex Sans, IBM Plex Mono) | Build reproducible y sin request a Google en runtime; el CDN deja la identidad de marca colgando de la red del usuario, y el `<link>` del canvas es un detalle de prototipo, no un requisito |
| Buscador por SKU | **Sí** se implementa la regla del canvas: `^PRD-\d{3}$` → navegación directa a la ficha | Es 100 % frontend, no toca la API, y el placeholder “Nombre o SKU” del diseño la promete |
| Validación funcional | **Chrome DevTools MCP** contra la app corriendo, con guion escrito (fase 8) | Decisión del usuario: sin Playwright. Las pruebas automatizadas del repo son unit/component |
| Estado | Signals, `inject()`, `input()`/`output()`, control flow `@if/@for/@empty`, `OnPush` en todo | Angular moderno, no Angular de 2020 |
| `pageSize` | **10**, fijo. Sin selector | Decisión del usuario. Es explícito en la petición (el default de la API es 6) y cae dentro del rango `[1, 50]` que el servidor acota |
| Marca en el header | Solo **`TheOffice`** | Se descarta el tagline `CATÁLOGO DE ABASTECIMIENTO · SOLO CONSULTA` que dibuja el canvas |
| Título y favicon | `<title>` = `TheOffice — Catálogo`; favicon SVG con el monograma **TO** blanco sobre `#10243D` | El default del CLI (`TheOfficeWeb`, favicon de Angular) no es entregable |

**Fuera de alcance, explícito:** carrito, checkout, autenticación, backoffice, reseñas, favoritos,
i18n, selector de ordenamiento, contadores por categoría, cualquier mención a IVA.

---

## 3. Tokens del diseño → Tailwind

Extraídos del artboard **A · Sistema de diseño**. Esta tabla es la fuente para el `@theme`; no hace
falta volver a abrir el canvas para implementar.

### Paleta

| Token | Hex | Uso en el canvas |
|---|---|---|
| `primary-900` | `#10243D` | Marca, header, títulos |
| `primary-700` | `#1B3A61` | Botones primarios, enlaces |
| `primary-500` | `#2A5A94` | Foco, hover |
| `primary-100` | `#E3EBF4` | Chip de categoría activo |
| `accent-600` | `#C2761A` | Acentos de marca |
| `accent-100` | `#F6E7D2` | Avisos suaves |
| `surface` | `#FFFFFF` | Tarjetas, paneles |
| `surface-muted` | `#F6F7F9` | Fondo de página, botón secundario |
| `skeleton` | `#E9ECF1` | Skeletons y marcador “sin imagen” |
| `border` | `#DDE2E8` | Bordes por defecto |
| `border-strong` | `#CBD2DB` | Bordes de control |
| `text` | `#16202B` | Texto principal (14.6:1) |
| `text-body` | `#3C4A59` | Párrafos, texto de control |
| `text-muted` | `#5A6672` | Secundario, captions (5.9:1) |
| `text-disabled` | `#98A2AE` | Botón deshabilitado |
| `text-on-primary-muted` | `#B9C6D6` | Tagline sobre header oscuro |

### Estados de stock (fondo / texto / borde) — **nunca solo color: siempre símbolo + texto**

| Estado | Regla | Símbolo | bg | fg | border |
|---|---|---|---|---|---|
| Disponible | `stock > 10` | `●` | `#E4F1E9` | `#12603D` | `#B7DAC7` |
| Quedan pocas | `1 ≤ stock ≤ 10` | `▲` | `#FBEFD9` | `#7A4A05` | `#E6C98E` |
| Agotado | `stock = 0` | `✕` | `#F9E6E6` | `#96201F` | `#E4B3B2` |
| Descontinuado | `isActive = false`, **solo en detalle** | `◼` | `#ECEFF3` | `#44505E` | `#CBD2DB` |

### Tipografía

- **Archivo** — títulos y precios · **IBM Plex Sans** — cuerpo/UI · **IBM Plex Mono** — SKU y datos.
- Escala: `display 40/700` · `h1 30/700` · `precio 24/700` · `h3-card 18/600` · `body 16/400` ·
  `ui 14/500` · `mono-sku 13/600` · `caption 12/400`.

### Espaciado, radios, sombras, foco

- Espaciado base 4: `4 · 8 · 12 · 16 · 24 · 32 · 48 · 64`.
- Radios: `sm 2px` · `md 4px` · `lg 6px` · `xl 8px`.
- Sombras: `sm = 0 1px 2px rgba(16,36,61,.06)` · `md = 0 4px 12px rgba(16,36,61,.10)`.
- Foco: `outline: 3px solid #2A5A94; outline-offset: 2px` sobre `:focus-visible`.

### Layout

- Listado desktop: grilla de **3 columnas**; tablet 2; móvil (360 px) **1 columna**.
  Con `pageSize = 10` la última fila queda incompleta (3+3+3+1): las tarjetas se estiran a lo ancho
  de su celda, no se centran ni se rellena con huecos falsos.
- Detalle desktop: dos columnas `600px 1fr` (imagen 3:2 / ficha); móvil apilado.
- La imagen conserva **proporción 3:2** también en el marcador “sin imagen”, para que el layout no salte.

> **Ojo con los mockups:** el canvas dibuja “Mostrando 1–6 de 16 · página 1 de 3” porque asume el
> `pageSize` de 6 por defecto de la API. Con la decisión de 10, los datos semilla dan
> **2 páginas** (10 + 6). Los números del canvas son ilustrativos; los reales vienen del servidor.

---

## 4. Contrato de la API — verificado contra el código

**Base URL en desarrollo:** `http://localhost:5226` (HTTP, no HTTPS). Se levanta con `make run` desde
la raíz; en `Development` aplica migraciones y siembra datos al arrancar. **Usar `v1`** — existe una
`v2` con galería y presentaciones que queda fuera de alcance. JSON en **camelCase**. CORS abierto en
desarrollo (`AllowAnyOrigin`): sin cabeceras ni credenciales.

### `GET /api/v1/products`

| Param | Default | Notas |
|---|---|---|
| `page` | `1` | Un valor `< 1` el servidor lo normaliza a `1` |
| `pageSize` | `6` | El servidor lo acota a `[1, 50]`. Pedir 200 devuelve 50. **El frontend manda 10** |
| `category` | — | **`slug`** (`papeleria`), no el nombre ni el `publicId` |
| `search` | — | Busca en nombre **y** descripción, `LIKE %texto%`, insensible a mayúsculas |

```json
{
  "items": [
    {
      "publicId": "PRD-001",
      "name": "Resma de papel carta 75g",
      "price": 18900,
      "imageUrl": "https://placehold.co/600x400/png?text=Resma%20de%20papel%20carta%2075g",
      "stock": 120,
      "categoryName": "Papeleria",
      "categorySlug": "papeleria"
    }
  ],
  "page": 1,
  "pageSize": 6,
  "totalItems": 16,
  "totalPages": 3
}
```

- Solo devuelve productos **activos** (`isActive = true`). No hay que filtrar en cliente.
- **Ordenado por nombre ascendente, siempre.** No hay parámetro de orden.
- `imageUrl` puede venir **cadena vacía**.
- `totalPages` es campo calculado del servidor: se usa, no se recalcula.
- `items: []` es respuesta **válida**, no un error.

### `GET /api/v1/products/{publicId}`

```json
{
  "publicId": "PRD-001",
  "name": "Resma de papel carta 75g",
  "description": "Resma de 500 hojas tamano carta, 75 gramos, blancura 96%.",
  "price": 18900,
  "imageUrl": "https://placehold.co/600x400/png?text=...",
  "stock": 120,
  "isActive": true,
  "category": {
    "publicId": "CAT-001",
    "name": "Papeleria",
    "slug": "papeleria",
    "description": "Papel, cuadernos y utiles de escritura para el dia a dia de la oficina."
  }
}
```

- SKU inexistente → **`404` sin cuerpo**. No hay JSON de error que parsear.
- `category` **puede ser `null`**.
- A diferencia del listado, **sí devuelve inactivos** (`isActive: false`).
- `description` es texto plano, sin HTML. Nunca `innerHTML`.

### `GET /api/v1/categories`

Array **plano**, sin envoltorio ni paginación: `[{ publicId, name, slug, description }, …]` con
`CAT-001..CAT-004`. Filtra por `slug`, muestra `name`.

### Datos semilla reales

16 productos, 4 categorías (4 cada una), precios de **9.800** a **1.250.000 COP**, stock de **8** a
**350**, **todos activos**. Ningún producto tiene stock 0 — el estado “agotado” se implementa igual,
porque el catálogo real sí lo tendrá, y solo las pruebas unitarias lo cubren.

Los nombres de categoría **no tienen tildes** en la base (`Papeleria`, `Tecnologia`, `Organizacion`):
se renderiza lo que llega, **no se corrige en el frontend**. Todas las imágenes son `placehold.co`
600×400: el resultado tiene que verse bien con placeholders grises.

Los nombres largos y los precios de 7 dígitos son el caso real —
`Organizador de escritorio 5 compartimentos` junto a `$ 1.250.000` no puede romper la tarjeta.

### Idioma y formato

Toda la interfaz en **español latinoamericano**, un solo idioma, sin selector ni `@angular/localize`.
Español neutro: “computador” antes que “ordenador”, “ustedes” y nunca “vosotros”. Precio en **COP**
con locale **`es-CO`**: `18900` → `$ 18.900`, punto de miles, sin decimales.

---

## 5. Lo que NO existe — no construirlo ni simularlo

Sin carrito, pedidos, checkout ni wishlist. Sin autenticación, cuentas, “Mi cuenta” ni avatar. Sin
backoffice ni formularios de creación (el `POST` existe en la API, pero está fuera de alcance). Sin
reseñas, calificaciones, recomendados ni historial. Sin i18n. Sin selector de ordenamiento. Sin
contadores junto a los chips de categoría (el servicio no los devuelve). **Ninguna mención a IVA:**
la API no dice si los precios lo incluyen y el equipo no lo ha definido.

Si el diseño pidiera algo de esa lista, está fuera de alcance. Una pantalla honesta de solo lectura
es el entregable correcto.

## 6. Restricciones duras

- **Cero cambios** bajo `src/Domain`, `src/Application`, `src/Infrastructure` y
  `src/Presentation/TheOffice.Api`. El trabajo sobre el backend es cero.
- **No modificar** `src/TheOffice.sln`, `Directory.Packages.props` ni `Directory.Build.props`.
- **No agregar el proyecto Angular al `.sln`**, ni un `.csproj` envoltorio. Queda como proyecto Node
  independiente; su integración con las compuertas pasa por el `Makefile`.
- **No cambiar el contrato de la API** para acomodar el frontend. Lo que falte (ordenamiento, un
  campo, un filtro) se documenta como brecha en el resumen final; no se abre PR al backend.
- Del `Makefile`, `ci.yml`, `README.md` de la raíz, `AGENTS.md` y `lefthook.yml`, cambiar **solo** lo
  que este plan autoriza. Nada de refactorizar de paso.
- No agregar autenticación, ni un stub. Es un hueco deliberado del roadmap.
- No commitear credenciales ni archivos `.env`.
- Leer `AGENTS.md` de la raíz antes de empezar: tiene las reglas no obvias del repositorio.

---

## Fase 0 — Línea base y verificación del contrato

**Objetivo:** empezar desde verde y con el contrato confirmado a mano, no de memoria.

### Tareas
1. `make check` desde la raíz. Si viene roja, **detenerse y reportarlo** antes de tocar nada.
2. `make run` y verificar los tres endpoints con `curl` (o `/scalar`):
   - `GET /api/v1/products` (default: `page=1`, `pageSize=6`, `totalItems=16`, `totalPages=3`)
   - `GET /api/v1/products?category=papeleria&search=papel&page=2`
   - `GET /api/v1/products/PRD-001` y `GET /api/v1/products/PRD-042` (debe ser `404` **sin cuerpo**)
   - `GET /api/v1/categories` (array plano, 4 elementos, sin tildes)
3. Guardar las respuestas reales como fixtures en el scratchpad: son los datos de las pruebas unitarias.
4. Confirmar `node -v` y decidir el pin del `.nvmrc` (hoy local: `22.22.2`).

### Validación
- `make check` en verde **antes** de cualquier cambio.
- Las 5 respuestas capturadas coinciden con el contrato de la sección 4; toda divergencia se anota
  aquí antes de seguir.

### Commit
Ninguno (fase de verificación).

---

## Fase 1 — Andamiaje Angular y enganche a `make check` / CI

**Objetivo:** que el frontend exista, esté vacío y **ya sea ciudadano de primera** en la señal única
del repo. Enganchar las compuertas primero evita descubrir en la fase 8 que CI no lo corre.

### Tareas
1. `npx @angular/cli@latest new` en `src/Presentation/TheOffice.Web/` — standalone, sin SSR, con routing.
   **No** agregarlo a `src/TheOffice.sln` ni crear `.csproj` envoltorio.
2. `ng add @angular/eslint`. Prettier + `prettier-plugin-tailwindcss` (orden de clases) y
   `eslint-config-prettier` para que no se peleen.
3. Scripts en `package.json`, con estos nombres exactos porque el `Makefile` los invoca:
   `lint`, `build` (producción), `test:ci` (una corrida, headless, sin watch).
4. `<title>` = `TheOffice — Catálogo` en `index.html`, `lang="es-CO"` en el `<html>`, y favicon
   SVG con el monograma **TO** blanco sobre `#10243D` (reemplaza el de Angular).
5. `.nvmrc` con la versión de Node. `.gitignore` local para `dist/` y `.angular/`
   (`node_modules/` ya está ignorado en la raíz — verificar antes de duplicar).
6. **Makefile** — agregar objetivos sin reescribir los existentes, y extender los prerrequisitos:

   ```make
   WEB := src/Presentation/TheOffice.Web

   web-install:  ## Install frontend dependencies (locked, what CI runs)
   	npm ci --prefix $(WEB)

   web-lint:     ## Verify frontend style
   	npm run lint --prefix $(WEB)

   web-build:    ## Build the frontend
   	npm run build --prefix $(WEB)

   web-test:     ## Run frontend tests (headless, single run)
   	npm run test:ci --prefix $(WEB)

   web: web-install web-lint web-build web-test  ## Every frontend gate
   ```

   Luego `check: restore lint build test web` y `ci: restore-locked lint build test web secrets`.
   Sumarlos a `.PHONY` y dejarles el comentario `##`, que es de donde `make help` saca su salida.
7. **CI** (`.github/workflows/ci.yml`) — un paso **antes** de `Run the same gates as local`:

   ```yaml
   - name: Install Node.js
     uses: actions/setup-node@v6
     with:
       node-version-file: src/Presentation/TheOffice.Web/.nvmrc
       cache: npm
       cache-dependency-path: src/Presentation/TheOffice.Web/package-lock.json
   ```

   Nada de pasos que ejecuten npm directamente: el workflow delega todo a `make ci` a propósito,
   para que no se desincronice del `Makefile`. Este paso solo pone Node en PATH.
8. **README de la raíz** — mover Node de “instrumental del agente” a **requisito de compilación**,
   y decir en voz alta el costo: a partir de aquí un cambio de una línea en C# paga un `npm ci`.
9. **Lefthook** (`lefthook.yml`) — agregar al `pre-commit` un comando espejo del de C#, para los
   archivos staged del frontend: `glob: "*.{ts,html,css}"` corriendo Prettier en modo verificación
   sobre `{staged_files}`, con `fail_text` que apunte a `npm run format`. Mismo espíritu que el
   `format` existente: falla rápido y barato, antes de llegar a `make check` en `pre-push`.
10. Commitear `package-lock.json`.

### Validación
- `make help` lista los objetivos nuevos con su descripción.
- El hook nuevo dispara: `git add` de un `.ts` mal formateado y `git commit` **falla**; tras
  `npm run format` y re-stage, pasa.
- **`make check` pasa en verde desde la raíz**, con el proyecto Angular recién generado dentro
  (la prueba por defecto del CLI cuenta como señal de que el runner funciona).
- `make ci` pasa localmente (incluye `restore-locked` y `gitleaks`).
- El paso de Node en CI se verifica **en CI**, no solo en la máquina local.

### Commit
`build(web): andamiaje Angular y enganche a make check y CI`

---

## Fase 2 — Fundaciones: tokens, Tailwind, tipografía y locale

**Objetivo:** que las clases nombren intención (`bg-surface`, `text-muted`) y que los formatos sean
colombianos. Un valor arbitrario suelto (`bg-[#f4f4f5]`) es señal de que falta un token.

### Tareas
1. Instalar Tailwind v4 (`tailwindcss`, `@tailwindcss/postcss`) y configurar el postcss del build.
2. Volcar **toda la tabla de la sección 3** en un bloque `@theme` de `styles.css`: colores,
   familias tipográficas, radios, sombras. La escala de espaciado base-4 ya es la de Tailwind.
3. Estilos base: `body` con `bg-surface-muted` y `text-text`; `:focus-visible` global con el
   outline de 3px `primary-500` y offset 2px; `-webkit-font-smoothing: antialiased`.
4. `@fontsource/archivo`, `@fontsource/ibm-plex-sans`, `@fontsource/ibm-plex-mono` — solo los pesos
   que usa el diseño (400/500/600/700 según familia), importados desde `styles.css`.
5. `app.config.ts`: `provideHttpClient(withFetch())`, `provideRouter(routes, withComponentInputBinding())`,
   `{ provide: LOCALE_ID, useValue: 'es-CO' }` y `registerLocaleData(localeEsCO)`.
6. Un helper/pipe de precio que produzca `$ 18.900` (sin decimales, punto de miles).

### Validación
- **Prueba unitaria del formato de precio**: `18900 → "$ 18.900"`, `1250000 → "$ 1.250.000"`,
  `9800 → "$ 9.800"`. Es la prueba de que `LOCALE_ID` quedó registrado; sin ella el bug aparece
  como `$18,900.00` en la fase 5.
- `make check` verde.
- Inspección: `grep` de valores arbitrarios `\[#` en las plantillas → **debe dar cero**.

### Commit
`feat(web): tokens del diseño en Tailwind, tipografía y locale es-CO`

---

## Fase 3 — Capa de datos: modelos y `CatalogService`

**Objetivo:** un único punto tipado de contacto con la API, con los fallos como valores, no como
excepciones que se escapen a la plantilla.

### Tareas
1. `proxy.conf.json`: `/api` → `http://localhost:5226`, enganchado al target de `serve`.
   `src/environments/` guarda la base para otros entornos. **Cero URLs absolutas en el código.**
2. Interfaces que espejen los DTOs en camelCase: `ProductListItem`, `PagedResult<T>`,
   `ProductDetail`, `Category`. `category` de `ProductDetail` es `Category | null`.
3. `CatalogService` con `inject(HttpClient)`:
   - `getProducts({ page, pageSize, category, search })` — omite params vacíos, no manda `search=`.
     `pageSize` por defecto **10**, definido como constante del frontend (no se confía en el
     default de 6 del servidor).
   - `getProduct(publicId)` — `404` se traduce a un resultado “no encontrado”, no a un throw.
   - `getCategories()` — array plano.
4. Discriminar **tres** desenlaces: éxito, no encontrado (`404`) y **error de red**
   (`HttpErrorResponse.status === 0`, la API caída). El diseño exige mensajes distintos y ningún
   código HTTP en pantalla para el tercero.
5. `totalPages` se **usa**, no se recalcula.

### Validación / pruebas (`HttpTestingController`)
- `getProducts` arma la URL con `page`, `pageSize=10`, `category` (slug) y `search`, y **omite**
  los vacíos.
- `getProducts` con `items: []` devuelve éxito con lista vacía, no error.
- `getProduct` con `404` sin cuerpo → resultado “no encontrado”.
- `getProduct`/`getProducts` con `status: 0` → resultado “error de red”.
- `getCategories` mapea el array plano.
- `afterEach` con `httpMock.verify()`.
- `npm run test:ci` verde; `make check` verde.

### Commit
`feat(web): servicio de catálogo tipado con 404 y error de red diferenciados`

---

## Fase 4 — Biblioteca de componentes del sistema de diseño

**Objetivo:** construir los componentes sueltos del artboard A antes que las pantallas, para que las
pantallas sean composición y no CSS a mano. Todo `standalone`, `OnPush`, con `input()`/`output()`.

### Tareas
| Componente | Responsabilidad | Detalle que no se puede omitir |
|---|---|---|
| `StockBadge` | Traduce `stock` a los 3 estados | Símbolo **+ texto** además del color. Umbrales `>10` / `1–10` / `0` |
| `DiscontinuedBadge` | Estado `◼ Descontinuado` | Solo se usa en el detalle. Nunca en tarjeta |
| `ProductImage` | Imagen o marcador “sin imagen” | Mismo marcador para `imageUrl` vacío y para error de carga (`(error)`). Proporción 3:2, **nunca en rojo**: es estado permanente, no fallo |
| `CategoryChip` | Chip de filtro y chip enlazado | Variante activa (`primary-100`) con `✕` para quitar. **Sin contadores** |
| `SearchField` | Campo de búsqueda, 44 px de alto | Placeholder “Nombre o SKU (ej. PRD-001)”. Emite el término ya *debounced* |
| `Pagination` | Páginas numeradas + anterior/siguiente | `aria-current="page"`, extremos deshabilitados, leyenda “Mostrando 1–6 de 16” |
| `ProductCard` | Tarjeta del listado | imagen, etiqueta de categoría sobre la imagen (**se omite si no hay categoría**), SKU mono, nombre, precio, badge de stock, enlace “Ver detalle →” |
| `SkeletonCard` | Placeholder de carga | Misma altura que `ProductCard` para que la grilla no salte |
| `EmptyState` / `ErrorState` | Bloques de mensaje con acciones | Botones reales, no texto |
| `Button` | Primario / secundario / texto / deshabilitado | Deshabilitado usa `text-disabled`, no opacidad |

Accesibilidad **en esta fase, no después**: roles correctos, `alt` significativo,
`aria-label` donde el marcado no alcance, foco visible heredado del `:focus-visible` global,
área táctil ≥ 44 px en controles.

### Validación / pruebas
- `StockBadge`: los 3 umbrales producen símbolo, texto y clases correctos — incluido `stock = 0`,
  que **no existe en los datos semilla** y por eso solo la prueba lo cubre.
- `ProductImage`: `imageUrl: ''` renderiza el marcador; disparar `(error)` sobre la `<img>` también.
- `ProductCard`: sin categoría, la etiqueta sobre la imagen no se renderiza.
- `Pagination`: página 1 deshabilita “Anterior”; última deshabilita “Siguiente”; `aria-current`
  cae en la página activa.
- `Button` deshabilitado no emite el `output`.
- `make check` verde (incluye el orden de clases de Tailwind, que verifica Prettier).

### Commit
`feat(web): componentes base del sistema de diseño`

---

## Fase 5 — Pantalla de listado

**Ruta:** `/` y `/productos` (`loadComponent`, perezosa).

### Tareas
1. Estado en **signals**; los filtros viven en los **query params de la URL**
   (`page`, `category`, `search`) para que el resultado sea compartible y “atrás” funcione.
2. Búsqueda con *debounce* ~300 ms. Cambiar término **o** categoría **reinicia a la página 1**.
3. **Regla del SKU** (del canvas): si el término calza `^PRD-\d{3}$` → `router.navigate` al detalle.
   Cualquier otro texto → `search` a la API. Un SKU parcial (`PRD-0`) no encuentra nada, y el
   estado vacío lo dice.
4. Encabezado de marca: solo **`TheOffice`** sobre `primary-900`; **sin tagline**.
5. Encabezado de la página: `{{ totalItems }} referencias activas · orden alfabético` — **valor
   dinámico del servidor**, jamás una cifra escrita a mano. Leyenda “Mostrando X–Y de N · página
   P de T”, calculada desde `page`, `pageSize` y `totalItems` que devuelve la respuesta.
6. Chips de categoría: “Todas” + las 4 del servicio. Filtra por `slug`, muestra `name` tal cual llega
   (sin tildes: no “corregir” en el frontend).
7. Estados, todos implementados, ninguno improvisado:
   - **Cargando** → grilla de `SkeletonCard` (no spinner centrado) + `role="status" aria-live="polite"`
     con “Cargando productos…”. Aplica en primera carga **y en cada cambio de filtro**.
   - **Vacío** → mensaje que nombra el término y la categoría, con “Limpiar filtros” y
     “Ver todas las categorías”, y la aclaración de que el SKU debe ser completo y exacto.
   - **Error de red** → “No pudimos cargar el catálogo”, **sin código HTTP**, botón **Reintentar**,
     y los filtros se conservan.
8. Anunciar el cambio de resultados a lectores de pantalla (región `aria-live`).
9. Grilla responsive 1 / 2 / 3 columnas. Debe aguantar “Organizador de escritorio 5 compartimentos”
   junto a “$ 1.250.000” sin romperse.

### Validación / pruebas
- Componente con `CatalogService` mockeado (NSubstitute no aplica aquí: mock manual o `vi.fn`):
  - Con `items: []` renderiza el estado vacío y no la grilla.
  - Con error de red renderiza `ErrorState` y el botón reintentar dispara una nueva llamada.
  - Escribir `PRD-005` navega al detalle y **no** llama a `getProducts`.
  - Escribir `resma` llama a `getProducts` con `search=resma` y `page=1`.
  - Cambiar de categoría estando en la página 3 resetea `page` a 1.
  - El debounce agrupa pulsaciones rápidas en una sola llamada (test con reloj falso).
- `make check` verde.

### Commit
`feat(web): pantalla de listado con filtros, búsqueda y paginación en la URL`

---

## Fase 6 — Pantalla de detalle

**Ruta:** `/productos/:publicId`, con `withComponentInputBinding()` para recibir el SKU como `input()`.

### Tareas
1. Layout del artboard C: imagen 600×400 a la izquierda, ficha a la derecha; apilado en móvil.
2. Migas de pan `Catálogo / Mobiliario / PRD-005` y enlace “← Volver al listado” que **preserve los
   filtros** del listado.
3. **SKU prominente y copiable**: botón “Copiar SKU” con Clipboard API y confirmación “✓ Copiado”
   anunciada a lectores de pantalla.
4. Precio unitario (COP), disponibilidad con `StockBadge`, descripción como **texto plano**
   (nunca `innerHTML`), y ficha técnica `Categoría / Referencia / Estado`.
5. **Sin categoría** (`category: null`): la miga colapsa a `Catálogo / PRD-013`, el chip enlazado
   **no se dibuja** (no queda vacío) y la ficha técnica declara “Sin categoría asignada”.
6. Chip de categoría enlazado de vuelta al listado **ya filtrado por ese `slug`**.
7. Estados:
   - **404** → “No encontramos el producto PRD-042”, explica el formato del SKU, ofrece ir al listado.
   - **Error de red** → mismo tratamiento que en el listado, con reintentar.
   - **Agotado** (`stock = 0`) → aviso en la ficha, no solo el badge.
   - **Descontinuado** (`isActive: false`) → aviso gris `◼`, **no ocultamiento**. Se llega solo por
     URL directa, porque el listado no devuelve inactivos.
   - **Sin imagen** → mismo marcador de la fase 4, a escala grande.
8. **Sin CTA de compra.** El detalle informa, no vende.

### Validación / pruebas
- `category: null` → no hay chip, la miga tiene 2 niveles, la ficha dice “Sin categoría asignada”.
- `isActive: false` → se renderiza el aviso de descontinuado y **no** se oculta el producto.
- `stock: 0` → aviso de agotado en la ficha.
- Resultado “no encontrado” → estado 404 con el SKU pedido en el mensaje.
- El enlace de regreso conserva los query params del listado.
- No existe ningún botón de compra en el DOM renderizado (prueba de guardia contra el impulso).
- `make check` verde.

### Commit
`feat(web): ficha de detalle con estados 404, agotado, descontinuado y sin categoría`

---

## Fase 7 — Auditoría de accesibilidad, responsive y build

**Objetivo:** pasada transversal de verificación. La accesibilidad se implementó en las fases 4–6;
aquí se comprueba, no se empieza.

### Tareas
1. Recorrido completo **solo con teclado** en ambas pantallas: orden lógico, foco siempre visible,
   ningún control alcanzable sin foco ni trampa de foco.
2. Contraste verificado contra la tabla de la sección 3 (texto ≥ 4.5:1, controles ≥ 3:1).
3. Ningún estado comunicado **solo** por color — revisión de los 4 badges de stock.
4. Responsive real de **360 px a escritorio**, incluidos los casos difíciles: nombre largo +
   precio de 7 dígitos, chips de categoría que desbordan, paginación en móvil.
5. Zoom del navegador al 200 % sin scroll horizontal.
6. Revisar el peso del bundle de producción y los presupuestos del `angular.json`; ajustarlos con
   criterio si `@fontsource` los excede (y explicar el ajuste en el commit).

### Validación
- Checklist de teclado y contraste, punto por punto, con resultado escrito.
- `npm run build` sin warnings de presupuesto.
- `make check` verde.

### Commit
`fix(web): correcciones de accesibilidad y responsive tras la auditoría`

---

## Fase 8 — Batería de validación funcional (Chrome DevTools MCP) y documentación

**Objetivo:** el guion reproducible que demuestra que la implementación hace lo que promete, contra
la app y la API reales. Es la contraparte manual/agente de las pruebas unitarias, no su reemplazo.

> Requiere el MCP de Chrome DevTools configurado (lo hace el usuario al cerrar este plan) y dos
> procesos vivos: `make run` en la raíz y `npm start` en el frontend.

### Tareas
1. Escribir `docs/plans/frontend-validation-script.md`: cada escenario con **pasos, dato de entrada
   y criterio de aceptación observable**.
2. Ejecutarlo con el MCP y dejar registrado el resultado de cada escenario.

### Escenarios

| # | Escenario | Cómo se provoca | Criterio |
|---|---|---|---|
| 1 | Carga inicial | `/` con la API arriba | 10 tarjetas, “16 referencias activas”, “página 1 de 2” |
| 2 | Skeletons | Throttling de red lento en DevTools | Aparecen skeletons, no spinner; sin salto de layout al resolver |
| 3 | Filtro por categoría | Chip “Mobiliario” | URL con `?category=mobiliario`, 4 productos, una sola página, `page` reseteada |
| 4 | Búsqueda con debounce | Teclear “resma” rápido | **Una sola** petición en la pestaña Network |
| 5 | SKU directo | Teclear `PRD-005` | Navega a `/productos/PRD-005` sin pasar por `search` |
| 6 | SKU parcial | Teclear `PRD-0` | Estado vacío, con el consejo del código completo |
| 7 | Estado vacío | Buscar “grapadora industrial” en Papeleria | Mensaje que nombra término y categoría; “Limpiar filtros” funciona |
| 8 | URL compartible | Copiar la URL filtrada y abrirla en pestaña nueva | Reproduce filtro, búsqueda y página |
| 9 | Volver preservando filtros | Entrar al detalle desde la página 2 y volver | Regresa a la página 2 con los mismos filtros |
| 10 | Paginación | Ir a la página 2 | 6 productos, “Siguiente” deshabilitado, `aria-current` en 2 |
| 11 | 404 | Navegar a `/productos/PRD-042` | Estado 404 con el SKU en el mensaje, sin pantalla en blanco |
| 12 | Error de red | Detener `make run` y recargar | Mensaje sin código HTTP + “Reintentar”; al relanzar la API, reintentar carga |
| 13 | Sin imagen | Bloquear `placehold.co` en DevTools | Marcador 3:2, gris, sin layout roto y sin tratamiento rojo |
| 14 | Sin categoría | Interceptar la respuesta del detalle y poner `category: null` | Miga de 2 niveles, sin chip, ficha declara el vacío |
| 15 | Descontinuado | Interceptar y poner `isActive: false` | Aviso gris visible, producto **no** oculto |
| 16 | Agotado | Interceptar y poner `stock: 0` | `✕ Agotado` en tarjeta y aviso en ficha |
| 17 | Copiar SKU | Botón “Copiar SKU” | Portapapeles con `PRD-005` y confirmación visible |
| 18 | Móvil 360 px | Emulación 360×800 | Una columna, filtros visibles, sin scroll horizontal |
| 19 | Teclado | `Tab` de punta a punta | Foco visible siempre, orden lógico, todo alcanzable |
| 20 | Consola limpia | Recorrido completo | Cero errores en consola |

3. **README del frontend** (`src/Presentation/TheOffice.Web/README.md`): instalar, correr, que
   necesita `make run` en la raíz, y **el mapeo tokens del diseño → configuración de Tailwind**.
4. **`AGENTS.md` de la raíz** — sección de frontend con sus reglas no obvias, en el mismo tono del
   resto del archivo (solo lo que no se deduce leyendo el código):
   - el frontend **no** está en el `.sln` y es invisible para `dotnet build`/`format`;
   - `make check` ahora exige Node y paga un `npm ci`;
   - `pageSize` es 10 por decisión del frontend, no el 6 del servidor;
   - los nombres de categoría llegan **sin tildes** y no se corrigen en el cliente;
   - la regla del buscador por SKU vive solo en el cliente;
   - cero valores arbitrarios de Tailwind: si falta un color, falta un token;
   - los huecos deliberados (sin carrito, sin auth, sin i18n, sin selector de orden) también
     aplican al frontend.
5. Resumen final con las **brechas del contrato** encontradas (p. ej. la API no ordena ni cuenta por
   categoría), documentadas como brechas, **sin abrir PR al backend**.

### Validación
- Los 20 escenarios con resultado registrado.
- `make check` verde desde la raíz — **esa es la prueba de que el trabajo está completo**, no
  “compila en mi carpeta”.

### Commit
`docs(web): README del frontend y guion de validación funcional`

---

## Contrato de calidad transversal

Aplica a **todas** las fases; no se “deja para el final”:

- `make check` en verde al cerrar cada fase. Ninguna fase se commitea en rojo.
- Sin `any` en el código de aplicación. Sin `innerHTML`. Sin URLs absolutas de API.
- `OnPush` en todo componente; `inject()` en vez de constructor injection; `input()`/`output()`
  como funciones; control flow `@if`/`@for`/`@empty`.
- Cero valores arbitrarios de Tailwind (`bg-[#…]`): si falta un color, falta un token.
- Nombres de prueba en **inglés**, patrón `Method_Scenario_ExpectedResult` — es la convención del
  repo y aplica también al frontend.
- Nada que la API no soporte: sin selector de orden, sin contadores por categoría, sin mención de IVA.
- Restricciones duras respetadas: **cero cambios** bajo `src/Domain`, `src/Application`,
  `src/Infrastructure`, `src/Presentation/TheOffice.Api`, `src/TheOffice.sln`,
  `Directory.Packages.props`, `Directory.Build.props`.

---

## Riesgos conocidos

| Riesgo | Mitigación |
|---|---|
| Tailwind v4 no encaja con el builder de la versión instalada de Angular | Se detecta en la fase 2; caída documentada a v3 con `tailwind.config.js` |
| El runner por defecto del CLI resulta ser Karma | `--watch=false --browsers=ChromeHeadless` y **verificarlo en CI**, no solo en local |
| `make check` se vuelve lento por el `npm ci` en cada corrida | Es el costo aceptado de la señal única; si molesta, se discute un objetivo con guard de timestamp (fuera de este plan) |
| El canvas y este plan divergen | Regla fijada arriba: el canvas manda sobre lo visual, este plan sobre el alcance |

---

## Preguntas sin resolver

Ninguna. Las seis preguntas de la primera versión quedaron cerradas:

| # | Pregunta | Respuesta |
|---|---|---|
| 1 | ¿Sección de frontend en `AGENTS.md`? | **Sí** — fase 8 |
| 2 | ¿Hook de Lefthook para el frontend? | **Sí** — fase 1 |
| 3 | Marca en el header | Solo `TheOffice`, sin tagline |
| 4 | `pageSize` | **10**, fijo, sin selector |
| 5 | Título de pestaña y favicon | `TheOffice — Catálogo` + monograma **TO** sobre `#10243D` |
| 6 | ¿Publicar como artifact? | Se queda como `.md` en `docs/plans/`, como el plan de ARKWS-40 |
