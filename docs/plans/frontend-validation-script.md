# Guion de validación funcional — Frontend del catálogo

Contraparte manual/agente de las pruebas unitarias, **no su reemplazo**. Las unitarias prueban
piezas contra dobles; esto prueba la aplicación entera contra la API real, con el navegador de
verdad. Cubre la fase 8 del [plan del frontend](./frontend-plan.md).

## Preparación

Dos procesos vivos, en dos terminales:

```bash
make dev    # API en http://localhost:5226 y app en http://localhost:4200
```

O en dos terminales, con `make run` y `make web-run`.

El frontend habla con `/api/v1` en su propio origen; `proxy.conf.json` lo reenvía al `5226`. Si la
API no está arriba, todos los escenarios caen en el estado de error — que es exactamente lo que
comprueba el escenario 12, y nada más.

Se ejecuta con el **MCP de Chrome DevTools**. Cada escenario deja registrado su resultado en la
tabla del final.

Datos semilla de referencia: **16 productos activos**, **4 categorías** (4 productos cada una),
precios de `9.800` a `1.250.000`, stock de `8` a `350`. Con `pageSize = 10` son **2 páginas**
(10 + 6). Ningún producto sembrado tiene `stock = 0` ni `isActive = false`: esos dos estados se
provocan interceptando la respuesta.

---

## Escenarios

### 1 · Carga inicial

**Pasos:** abrir `http://localhost:4200/` con la API arriba.
**Criterio:** se pintan **10 tarjetas**; el encabezado dice **`16 referencias activas · orden
alfabético`**; la paginación dice **`página 1 de 2`**; la primera tarjeta es
`Archivador metalico 4 gavetas` (orden alfabético ascendente, viene del servidor).

### 2 · Skeletons durante la carga

**Pasos:** activar throttling de red lento (`Slow 3G`) en DevTools y recargar.
**Criterio:** aparece una **grilla de skeletons**, no un spinner centrado. Al resolver, la grilla
**no salta**: los skeletons ocupan la misma altura que las tarjetas. El `role="status"` anuncia
`Cargando productos…`.

### 3 · Filtro por categoría

**Pasos:** pulsar el chip `Mobiliario`.
**Criterio:** la URL pasa a `?category=mobiliario`; se ven **4 productos**; la paginación desaparece
o declara una sola página; el parámetro `page` queda reseteado (no aparece `page=3` heredado).

### 4 · Búsqueda con debounce

**Pasos:** con la pestaña Network abierta, teclear `resma` rápido (sin pausas).
**Criterio:** **una sola** petición a `/api/v1/products` con `search=resma`, no cinco.

### 5 · Búsqueda por SKU exacto

**Pasos:** teclear `PRD-005` en el buscador.
**Criterio:** navega a `/productos/PRD-005`. En Network **no** hay una petición de listado con
`search=PRD-005`: la regla es 100 % del cliente.

### 6 · SKU parcial

**Pasos:** teclear `PRD-0`.
**Criterio:** no navega; se pide el listado con `search=PRD-0`; se muestra el **estado vacío**, que
aclara que el código debe ir **completo y exacto**.

### 7 · Estado vacío con filtros

**Pasos:** elegir la categoría `Papeleria` y buscar `grapadora industrial`.
**Criterio:** el mensaje **nombra el término y la categoría**. `Limpiar filtros` devuelve al listado
completo y limpia la URL.

### 8 · URL compartible

**Pasos:** con un filtro, una búsqueda y una página aplicados, copiar la URL y abrirla en una
pestaña nueva.
**Criterio:** la pestaña nueva reproduce **filtro, búsqueda y página**, sin estado previo.

### 9 · Volver preservando filtros

**Pasos:** ir a la página 2, entrar a un detalle, pulsar `← Volver al listado`.
**Criterio:** regresa a la **página 2** con los mismos filtros. El botón «atrás» del navegador hace
lo mismo.

### 10 · Paginación

**Pasos:** pulsar `Siguiente` desde la página 1.
**Criterio:** **6 productos**; `Siguiente` deshabilitado; `Anterior` habilitado; `aria-current="page"`
sobre el `2`; leyenda `Mostrando 11–16 de 16 · página 2 de 2`.

### 11 · Producto inexistente (404)

**Pasos:** navegar directo a `/productos/PRD-042`.
**Criterio:** estado 404 que **nombra el SKU pedido**, explica el formato (`PRD-` + 3 dígitos) y
ofrece volver al listado. Nada de pantalla en blanco ni de código HTTP suelto.

### 12 · API caída

**Pasos:** detener `make run` y recargar `/`.
**Criterio:** `No pudimos cargar el catálogo`, **sin ningún código HTTP** en pantalla, con botón
`Reintentar`. Los filtros de la URL se conservan. Al relanzar la API, `Reintentar` carga sin
recargar la página.

### 13 · Producto sin imagen

**Pasos:** bloquear `placehold.co` en DevTools (Network → Block request domain) y recargar.
**Criterio:** marcador **`Sin imagen`** en gris, proporción **3:2** conservada, layout intacto, y
**ningún tratamiento rojo**: es un estado permanente, no un fallo.

### 14 · Producto sin categoría

**Pasos:** interceptar la respuesta de `/api/v1/products/PRD-013` y forzar `"category": null`.
**Criterio:** la miga de pan colapsa a `Catálogo / PRD-013` (dos niveles); **no** se dibuja el chip
de categoría; la ficha técnica declara `Sin categoría asignada`.

### 15 · Producto descontinuado

**Pasos:** interceptar la respuesta del detalle y forzar `"isActive": false`.
**Criterio:** aviso gris `◼ Descontinuado` visible; el producto **no** se oculta ni redirige.

### 16 · Producto agotado

**Pasos:** interceptar y forzar `"stock": 0` en listado y detalle.
**Criterio:** `✕ Agotado` en la tarjeta y **aviso en la ficha**, no solo el badge.

### 17 · Copiar SKU

**Pasos:** en `/productos/PRD-005`, pulsar `Copiar SKU`.
**Criterio:** el portapapeles contiene `PRD-005`; aparece la confirmación `✓ Copiado`, anunciada por
`aria-live`.

### 18 · Móvil 360 px

**Pasos:** emular 360×800.
**Criterio:** **una columna**; buscador y chips visibles y usables; **sin scroll horizontal** en
ninguna de las dos pantallas.

### 19 · Recorrido de teclado

**Pasos:** `Tab` de punta a punta en listado y detalle.
**Criterio:** foco **siempre visible**, orden lógico, todos los controles alcanzables, sin trampas
de foco. `Enter` activa chips, paginación y enlaces.

### 20 · Consola limpia

**Pasos:** repetir el recorrido completo con la consola abierta.
**Criterio:** **cero errores**. Los avisos de terceros (extensiones del navegador) se anotan pero no
cuentan.

---

## Registro de ejecución

Ejecutado el **2026-08-29** con el MCP de Chrome DevTools contra `make dev`, en
Chrome de escritorio (1440×900) y en emulación móvil (360×800). Los estados que los datos semilla
no producen (`stock = 0`, `isActive = false`, `category: null`, `imageUrl` vacío) se provocaron
interceptando `fetch` con un `initScript` en la navegación.

| # | Escenario | Resultado | Nota |
|---|---|---|---|
| 1 | Carga inicial | ✅ | 10 tarjetas · `16 referencias activas · orden alfabético` · `página 1 de 2` · primera tarjeta `Archivador metalico 4 gavetas` |
| 2 | Skeletons | ✅ | Con `Slow 3G`: 10 `SkeletonCard` y `role="status"` con `Cargando productos…`. Ningún spinner en el DOM |
| 3 | Filtro por categoría | ✅ | `?category=mobiliario`, 4 productos, `página 1 de 1`, `page` fuera de la URL |
| 4 | Búsqueda con debounce | ✅ | Ráfaga de 5 pulsaciones → **una sola** petición `products?page=1&pageSize=10&search=resma` |
| 5 | SKU directo | ✅ | `PRD-005` → `/productos/PRD-005`. Ninguna petición de listado con ese término |
| 6 | SKU parcial | ✅ | `PRD-0` → estado vacío con el consejo del código completo y exacto |
| 7 | Estado vacío | ✅ | `No encontramos productos para «grapadora industrial» en Papeleria.` · `Limpiar filtros` devuelve a `/productos` con 10 tarjetas |
| 8 | URL compartible | ✅ | `?category=mobiliario&search=silla&page=1` en pestaña limpia: campo relleno, chip activo, 1 resultado |
| 9 | Volver preservando filtros | ✅ **tras corrección** | Falló en la primera pasada: la tarjeta no propagaba los query params y volvía a la página 1. Corregido con `queryParamsHandling="preserve"`; ahora `/productos/PRD-015?page=2` → `/productos?page=2` |
| 10 | Paginación | ✅ | 6 productos · `Siguiente` deshabilitado · `aria-current="page"` en el `2` · `Mostrando 11–16 de 16 · página 2 de 2` |
| 11 | 404 | ✅ | `No encontramos el producto PRD-042`, formato del SKU explicado, botón al catálogo. Sin código HTTP |
| 12 | Error de red | ✅ | Con la red en `Offline`: `No pudimos cargar el catálogo`, sin código HTTP, `?category=papeleria` conservado. Al restaurar la red, `Reintentar` carga los 4 productos sin recargar |
| 13 | Sin imagen | ✅ | Marcador `Sin imagen`, proporción medida **1.5** (3:2), fondo `#E9ECF1` y texto `#5A6672`. Sin rojo, sin scroll horizontal |
| 14 | Sin categoría | ✅ | Miga de **2** niveles, cero enlaces a `category=`, ficha declara `Sin categoría asignada` |
| 15 | Descontinuado | ✅ | `◼ Descontinuado` con aviso, y el producto sigue visible |
| 16 | Agotado | ✅ | `✕ Agotado` en las 10 tarjetas y aviso propio en la ficha, además del badge |
| 17 | Copiar SKU | ✅ | Portapapeles = `PRD-005`, `✓ Copiado` visible y en la región `aria-live` |
| 18 | Móvil 360 px | ✅ | 1 columna, `scrollWidth == clientWidth`, ningún elemento desbordado, ningún control por debajo de 44 px. El detalle a 360 px tampoco desborda |
| 19 | Teclado | ✅ | Orden = orden del DOM (marca → buscador → chips → tarjetas → paginación), cero `tabindex` positivos o negativos, `outline: 3px solid rgb(42,90,148)` en cada parada |
| 20 | Consola limpia | ✅ **tras corrección** | Cero errores desde el principio. Había dos warnings `NG0913` (imagen del LCP en `loading="lazy"`); corregido con el input `priority`. Consola ahora vacía |

### Comprobaciones adicionales de la fase 7

| Comprobación | Resultado |
|---|---|
| Contraste de texto (16 pares) | ✅ Todos ≥ 4.5:1. El más bajo, `text-muted` sobre `surface-muted`, da 5.47:1 |
| Contraste de controles | ⚠️→✅ `border-strong` daba **1.52:1** sobre blanco. Nuevo token `border-control` (**3.37:1** sobre `surface`, 3.14:1 sobre `surface-muted`) en input, botón secundario, chips y paginación |
| `text-disabled` sobre `surface-muted` | 2.41:1 — **exento** por WCAG 1.4.3 (componentes de interfaz inactivos). Se deja como lo define el canvas |
| Ningún estado solo por color | ✅ Los 4 badges llevan símbolo (`●` `▲` `✕` `◼`) y palabra; el símbolo es `aria-hidden` |
| Zoom al 200 % (viewport 720×450) | ✅ Sin scroll horizontal; la grilla baja a 2 columnas |
| Presupuestos del bundle | ✅ 285 kB iniciales contra un límite de 500 kB. No hizo falta ajustarlos. Las dos pantallas son chunks perezosos (12.5 kB y 8.7 kB) |

### Defectos encontrados y corregidos

Seis, todos en el commit `fix(web): correcciones de accesibilidad y responsive tras la auditoría`:

1. La marca vivía dentro del listado → la ficha salía sin cabecera. Movida al shell.
2. La tarjeta no propagaba los filtros al detalle (escenario 9).
3. El chip `Todas` ofrecía `Quitar filtro de Todas`, una acción vacía anunciada como real.
4. `1 referencias activas` → singular/plural.
5. `NG0913`: imagen del LCP en `lazy` (escenario 20).
6. Borde de control con 1.52:1 de contraste.

### Brechas del contrato de la API

Documentadas, **sin abrir PR al backend** — el plan lo prohíbe explícitamente:

- **No hay parámetro de ordenamiento.** El servidor ordena siempre por nombre ascendente. Por eso el
  encabezado dice `orden alfabético` como un hecho y no hay selector de orden.
- **`GET /api/v1/categories` no devuelve conteos.** Por eso los chips no llevan contador: mostrarlo
  costaría una petición por categoría.
- **El `404` sí trae cuerpo** (`ProblemDetails` JSON), a diferencia de lo que decía el plan. No
  cambia nada: el cliente solo lee el status y nunca muestra el código.
- **El listado no expone `isActive`.** Coherente — solo devuelve activos —, pero significa que el
  estado «descontinuado» solo puede pintarse en el detalle.
- **No hay endpoint de búsqueda por SKU exacto.** La regla `^PRD-\d{3}$` → ficha vive en el cliente.
