Diseña el frontend del catálogo de **TheOffice**, un ecommerce **B2B de artículos de oficina**
(papelería, mobiliario, tecnología, organización). Necesito el diseño visual de **dos pantallas** y
la identidad de marca, que hoy no existe: invéntala tú.

Toda la interfaz va en **español latinoamericano**. Un solo idioma, sin selector. Los formatos de
número y moneda son los de Colombia (`es-CO`).

## Quién usa esto

Departamentos de compras y secretarias que abastecen oficinas. Compra recurrente, por catálogo, con
volumen. **No es consumo masivo ni descubrimiento.**

Consecuencias que quiero ver reflejadas en el diseño:

- **Piensan en códigos, no en nombres.** El SKU (`PRD-001`) es su identificador de trabajo. Tiene que
  ser visible y copiable, no letra chica.
- El patrón es **"encontrar el ítem que ya sé que necesito"**: filtro por categoría y búsqueda por
  texto. No hay feed de inspiración ni recomendados.
- **La disponibilidad importa antes de armar el pedido.** El stock se ve en el listado, no solo en el
  detalle.
- El escenario real es una secretaria reponiendo papel un martes por la mañana, con prisa.
  **Herramienta de trabajo, no vitrina.** Densidad de información legible por encima del espectáculo.

## Qué puede y qué no puede la API

Esto no es contexto técnico de adorno: **son los límites de lo que el diseño puede prometer.** Un
diseño que prometa algo de esta lista es un diseño que no se puede construir.

- La búsqueda hace coincidencia parcial de texto sobre **nombre y descripción**. **No busca sobre el
  SKU:** escribir `PRD-001` en el buscador devuelve cero resultados. La aplicación lo resuelve
  detectando el código y saltando directo a la ficha — ver "Comportamiento del buscador".
- El servicio de categorías devuelve nombre y slug. **No devuelve cuántos productos tiene cada
  categoría.** Nada de contadores junto a los chips de filtro.
- El listado **solo devuelve productos activos**. Un producto descontinuado nunca aparece en la
  grilla; solo se llega a él por URL directa. **El estado "descontinuado" existe únicamente en la
  ficha de detalle, jamás en una tarjeta.**
- El total de referencias del catálogo **llega del servidor y cambia**. No lo escribas como cifra
  fija en ningún encabezado: márcalo como valor dinámico.
- Cuando el servidor está caído, el navegador **no recibe ningún código HTTP**. No hay un 503 que
  mostrar: la petición simplemente no llega. El mensaje de error no puede depender de un código.
- Un producto puede venir **sin categoría asignada**.
- La URL de la imagen puede venir **vacía**.
- El orden del listado es fijo, alfabético por nombre. **No existe parámetro de ordenamiento**, así
  que no diseñes un selector de "ordenar por".
- **Nada se sabe sobre impuestos.** La API no dice si los precios llevan IVA y el equipo no lo ha
  definido. No escribas "sin IVA" ni "IVA incluido" en ninguna parte.

## Lo que NO existe y no debes diseñar

No hay carrito, ni pedidos, ni checkout, ni autenticación, ni cuentas, ni reseñas, ni favoritos, ni
recomendados, ni historial. **Nada de "Agregar al carrito", contador de carrito, "Mi cuenta" ni
avatar.** Es un catálogo de solo lectura: informa, no vende. Si sientes el impulso de agregar un CTA
de compra, no lo hagas — no hay backend que lo soporte.

## Identidad de marca

Créala tú, desde cero: nombre en tipografía, paleta, tono. Referencias del carácter que busco —
sobrio, confiable, un poco institucional, sin ser gris ni aburrido. Piensa más en herramienta
profesional que en tienda de moda.

**Entrégame los tokens explícitos**, porque se van a implementar en Tailwind: paleta con hex
(primario, superficie, texto, bordes, y los estados de stock), escala tipográfica con familia y
tamaños, escala de espaciado, radios y sombras.

## Restricción visual que manda sobre todo

**Todas las imágenes de producto son placeholders grises de 600×400** (`placehold.co`, con el nombre
del producto escrito encima). No hay fotografía real y no la habrá pronto.

El diseño **tiene que verse bien con placeholders**. No apuestes la jerarquía visual a imágenes
bonitas: apóyate en tipografía, color, espaciado y estructura. Muéstrame los mockups con esos
placeholders, no con fotos de stock — quiero ver la verdad.

## Pantalla 1 — Listado de productos

Grilla de tarjetas de producto. Cada tarjeta necesita: imagen, nombre, **SKU**, precio en COP, stock
y categoría.

- **El stock se lee de un vistazo.** Un número solo no basta: diferencia "disponible", "quedan pocas"
  y "agotado". **No lo comuniques solo con color** — necesita texto o forma también.
- Filtro de categoría y campo de búsqueda **visibles arriba**, no escondidos tras un menú.
  Los chips de categoría llevan **solo el nombre**, sin contador.
- **Paginación explícita** (páginas numeradas). Nada de scroll infinito.
- El encabezado puede decir cuántas referencias hay, pero como **valor dinámico** del servidor, no
  como número escrito a mano.

### Comportamiento del buscador

El campo puede invitar a buscar por nombre **o por SKU**, y esa promesa se cumple — pero por un
camino que hay que dejar escrito en el canvas, junto al componente de búsqueda, para que quien
implemente no lo descubra tarde:

> SKU completo (`PRD-000`) → navega directo a la ficha. Cualquier otro texto → busca en nombre y
> descripción. Un SKU parcial (`PRD-0`) no encuentra nada: la búsqueda no mira el código.

## Pantalla 2 — Detalle de producto

Imagen grande, nombre, SKU prominente y copiable, precio, stock, categoría (enlazada de vuelta al
listado filtrado) y descripción completa. Un camino claro de regreso al listado.

Sin CTA de compra.

## Estados — diséñalos, no los des por hechos

Son parte del entregable, no un extra:

| Estado | Cuándo pasa |
|---|---|
| Cargando | Primera carga y cada cambio de filtro. Prefiero *skeletons* sobre spinner centrado |
| Vacío | Búsqueda sin resultados o categoría sin productos. Con acción para limpiar filtros |
| No encontrado | SKU inexistente o mal escrito en la URL del detalle |
| Error de red | La API caída. Mensaje claro **con botón de reintentar**, y sin código HTTP |
| Agotado | Producto con stock 0, en tarjeta y en detalle |
| Descontinuado | Producto inactivo. **Solo en la ficha de detalle**, con aviso — nunca en tarjeta |
| Sin imagen | La URL llega vacía o el servicio de placeholders no responde |
| Sin categoría | El producto no tiene categoría asignada |

Dos precisiones sobre los dos últimos, que suelen quedarse fuera:

- **Sin imagen** es un **estado permanente del catálogo, no un error**: nada de tratamiento rojo.
  Necesita un marcador propio que conserve la proporción 3:2 para que el layout no salte, en las dos
  escalas que existen — la de tarjeta y la grande del detalle.
- **Sin categoría** afecta la miga de pan y el chip enlazado del detalle. La miga colapsa
  (`Catálogo / PRD-013` en vez de `Catálogo / Mobiliario / PRD-005`) y el chip **no se dibuja**, en
  vez de quedar vacío: no hay listado filtrado al que enlazar.

Si el consejo de un estado vacío o de un 404 menciona el SKU, que sea coherente con el buscador:
sugiere escribir el **código completo y exacto**, y aclara que uno parcial no arroja resultados.

## Datos reales — úsalos en los mockups, no inventes

16 productos, 4 categorías (4 productos cada una), precios de **9.800** a **1.250.000 COP**, stock
de **8** a **350**. Con 6 productos por página, el listado son **3 páginas**.

Formato de precio: `$ 18.900` (locale es-CO, punto de miles, sin decimales).

Las categorías se llaman, literalmente y **sin tildes** porque así están en la base de datos:
**Papeleria, Mobiliario, Tecnologia, Organizacion**. Respétalo, no lo "corrijas".

Productos reales para poblar los mockups — nota los extremos, que son el caso difícil:

| SKU | Nombre | Precio | Stock |
|---|---|---|---|
| PRD-001 | Resma de papel carta 75g | 18.900 | 120 |
| PRD-003 | Boligrafo tinta negra x12 | 9.800 | 350 |
| PRD-005 | Silla ergonomica con soporte lumbar | 689.000 | 25 |
| PRD-007 | Archivador metalico 4 gavetas | 745.000 | 8 |
| PRD-009 | Monitor 27 pulgadas QHD | 1.250.000 | 18 |
| PRD-011 | Diadema con cancelacion de ruido | 459.000 | 30 |
| PRD-013 | Organizador de escritorio 5 compartimentos | 54.900 | 75 |
| PRD-016 | Perforadora industrial 30 hojas | 97.500 | 22 |

Descripción de ejemplo para el detalle (texto plano, 1–2 frases, es el largo real):
*"Silla de malla transpirable con soporte lumbar ajustable y apoyabrazos 3D."*

**Los nombres largos y los precios de 7 dígitos son el caso real.** Que la tarjeta aguante
"Organizador de escritorio 5 compartimentos" junto a "$ 1.250.000" sin romperse.

## Requisitos técnicos del diseño

- **Responsive de 360 px a escritorio.** Quiero ver ambas pantallas en móvil y en desktop.
- **Accesibilidad como requisito:** contraste suficiente, foco visible en la navegación por teclado,
  y nada que se comunique únicamente por color.
- El diseño se va a implementar en **Angular con Tailwind CSS**. Mantén los valores dentro de una
  escala consistente y evita efectos que no se traduzcan a CSS estándar.

## Entregable

Un canvas con artboards para:

1. **Sistema de diseño**: paleta con hex, tipografía, espaciado, radios, sombras, tratamiento de
   foco, y los componentes sueltos (tarjeta de producto, indicador de stock, chip de categoría,
   campo de búsqueda con la regla del SKU anotada, paginación, botón)
2. Listado — desktop
3. Detalle — desktop
4. Detalle — variante **sin categoría**
5. Listado y detalle — móvil 360 px
6. Los ocho estados de la tabla de arriba

Empieza por el sistema de diseño y la identidad, y deriva las pantallas de ahí — no al revés.
