# ARKWS-40 — Galería de fotos y presentaciones de producto

Rama: `david/arkws-40-galeria-de-fotos-y-presentaciones-de-producto-sol-2026-0187`
Base: `main` @ `689af6b`

## Work list (Step A)

Seleccionados por el usuario: **ARKWS-41 → ARKWS-47** (funcionales).
Fuera de este run: **ARKWS-48, 49, 50, 51** (documentación) — no bloquean nada, van en otro pase.

| # | Item | Estado | Nota |
|---|---|---|---|
| 1 | **ARKWS-41 + ARKWS-46** | pendiente | **un solo entregable, un solo commit.** Modelo + seeder + la única migración |
| 2 | ARKWS-42 | pendiente | repositorio |
| 3 | ARKWS-43 | pendiente | DTOs v2 + derivación v1 |
| 4 | ARKWS-44 | pendiente | endpoints de lectura v2 |
| 5 | ARKWS-45 | pendiente | POST v2 |
| 6 | ARKWS-47 | pendiente | pruebas |

**Por qué 41 y 46 son un solo entregable.** ARKWS-46 advierte: *"coordina esto con la
migración de ARKWS-41 en lugar de generar dos que se pisen."* Además, entre uno y otro el
repo queda en un estado donde el modelo ya no tiene `ImageUrl` pero la BD sí, con la
columna `NOT NULL` sin default: `make run` y toda inserción se rompen. `make check` no lo
vería — no hay pruebas que abran la base — así que el commit intermedio pasaría los hooks
y dejaría el runtime roto. Se cierran juntos, y ambos issues se comentan en el mismo push.

## Decisiones

Las de ARKWS-40 (D1–D10) siguen vigentes. De este run:

| # | Decisión |
|---|---|
| D11 | `ProductSummaryV2Response` lleva **"Imagen anidada + disponibilidad agregada"**: `PrimaryImage` como `ProductImageResponse?`, más `Stock`, `IsAvailable`, `VariantCount`. El listado no trae la lista de presentaciones |
| D12 | En el detalle v2, `price` y `stock` a nivel de producto usan la **"Misma derivación que v1"** |
| ~~D13~~ | ~~Las presentaciones con `IsActive = false` se filtran~~ — **revertida por D16** |
| ~~D14~~ | ~~Backfill solo con `HasData`~~ — **revertida por D18** |
| D15 | `PublicId` de imagen **"Derivado del producto: PRD-005-IMG-1"**, numerado 1..N por `SortOrder`, generado por el servidor |
| D16 | **"Quitar IsActive de las presentaciones"** — `ProductVariant` no lleva el campo. Todas se devuelven siempre; `IsAvailable = Stock > 0` es la única marca |
| D17 | `v1.stock` **"Suma TODAS las presentaciones"** — v1 nunca supo que existiera un flag de actividad |
| D18 | **"Agregar el backfill SQL"** — `migrationBuilder.Sql` a mano antes del `DropColumn`, para las filas no sembradas |
| D19 | **"No sembrar; Comercial valida con un POST"** — los 16 productos quedan sin presentaciones y `PRD-005` sigue reportando `stock 25` en v1 |

**D16 desvía de ARKWS-41**, que enumera `IsActive` entre los campos de `ProductVariant`.
El campo se retira: sin endpoint de edición (D4) y sin `isActive` en el request de
creación, nada podría ponerlo en `false`, y el filtro que dependía de él sería código
inalcanzable. Además A4 del padre es explícita — *"nunca filtradas"*. Queda anotado para
ARKWS-51 (el ADR).

## Supuestos

| # | Supuesto | Fuente | Qué lo falsearía |
|---|---|---|---|
| A10 | `IsAvailable` = `Stock > 0`, en presentación y en producto (agregado) | D16 + ARKWS-43 | Que "disponible" pase a significar otra cosa |
| A12 | Las presentaciones **no** tienen imágenes propias | ARKWS-41 enumera los campos de `ProductVariant`: ninguno es de imagen; y D4 deja edición fuera | Que Comercial pida foto por color — sería otra migración |
| A13 | `GetByPublicId` usa `Include` (un viaje); `GetPaged` proyecta | ARKWS-42 | — |
| A14 | Los DTOs v2 van en `Application/DTOs/`, uno por archivo, incluidos los records anidados del request | `docs/architecture.md:112` | — |
| A15 | Los métodos v2 viven en el `ProductService` existente | ARKWS-43 | — |
| A16 | `ProductV2Controller` es archivo nuevo con `[ApiVersion("2.0")]` | ARKWS-44 lo deja a criterio | — |
| A17 | `POST /api/v2/products` responde **200 OK**, no 201 | `ProductController.cs:42-52` — se sigue el patrón de v1 | — |
| A18 | Borrado en cascada `Product → Images/Variants` | ARKWS-41 | — |
| A19 | Índice único **global** en los dos `PublicId` nuevos | ARKWS-41 | — |
| A20 | `Persistence/Models/Product.ImageUrl` también se retira | ARKWS-41 ("retira la columna `ImageUrl`") | — |
| A21 | Las 16 imágenes sembradas usan serie de GUID propia (`b1000000-…`), nunca la del producto | `ProductSeeder.cs:42` usa `b0000000-…` | — |
| A22 | Los índices únicos van por atributo `[Index]`, no Fluent API | Es el patrón real del repo (`Models/Product.cs:9`). **Desvía de la letra de ARKWS-41**, que dice "configuración Fluent API … índice único en cada `PublicId`" | — |
| A23 | Comentarios en español sin acentos; identificadores, mensajes de error y nombres de prueba en inglés | `TheOfficeDbContext.cs:27-28`, `ProductService.cs:53`, `AGENTS.md` §Pruebas | — |

## Veredicto de las pruebas existentes (Step B.5)

`tests/TheOffice.Application.Tests/Services/ProductServiceTests.cs` — **11 métodos, 16 casos, todos `update`.** Ninguno `delete`, ninguno `escalate`.

`BuildProduct` (líneas 208-223) asigna `ImageUrl` sobre la entidad de dominio; cuando A1
retire el campo el archivo deja de compilar. Los asserts sobre `imageUrl` y `stock` **no
cambian de valor** — se vuelven las pruebas de compatibilidad de D1. Renombrados a inglés
(`GetAll_NoFilters_ReturnsPageWithAllProducts`, `GetAll_NormalizesPaging`,
`GetByPublicId_ExistingProduct_ReturnsSuccessResult`, …); los comentarios de sección del
archivo se quedan en español (A23).

## Baseline de verificación

`scratchpad/v1-baseline-products.csv` — los 16 productos con su `ImageUrl` y `Stock`
leídos de `theoffice.db` antes de tocar nada. `PRD-005` = silla, `stock 25`, que por D19
**no cambia**.

## Firmas que se fijan aquí

```csharp
// ProductService — v2 al lado de las de v1, sin sufijo Async, sin CancellationToken
Task<PagedResult<ProductSummaryV2Response>> GetAllV2(ProductQuery query);
Task<Result<ProductV2Response>?>            GetByPublicIdV2(string publicId);
Task<Result<ProductV2Response>>             CreateV2(CreateProductV2Request request);

// IProductRepository — dos métodos nuevos; los tres actuales no cambian de firma
Task<(IReadOnlyList<ProductListItem> Items, int TotalItems)> GetPagedList(
  int page, int pageSize, string? categorySlug, string? search);
Task<IReadOnlyList<string>> FindExistingPublicIds(
  string productPublicId, IReadOnlyList<string> variantPublicIds);
```

`GetAll` sigue devolviendo `PagedResult<T>` **sin** envoltura `Result` (`ProductService.cs:27`).
Los nombres de DTO son los que fija ARKWS-43 (`ProductSummaryV2Response`, `ProductV2Response`),
no un sufijo `…ResponseV2`.

**La derivación, una sola expresión, usada en los dos caminos:**

```csharp
// imagen principal: marcada, si no la de menor SortOrder; desempate estable por PublicId
Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).ThenBy(i => i.PublicId)
      .FirstOrDefault()?.Url ?? string.Empty
// stock: suma de TODAS las presentaciones (D17), o el propio del producto si no tiene
Variants.Count == 0 ? product.Stock : Variants.Sum(v => v.Stock)
// galeria v2, mismo orden
Images.OrderBy(i => i.SortOrder).ThenBy(i => i.PublicId)
```

`FirstOrDefault`, nunca `First`: un producto sin imágenes debe dar `""`, no un 500 en toda
la página (y ADR-0003 prohíbe excepciones para estados esperados).

## Pasos

### Item 1 · ARKWS-41 + ARKWS-46 — modelo, backfill y migración

1. Entidades de dominio `ProductImage` (`Id`, `PublicId`, `Url`, `SortOrder`, `IsPrimary`,
   `ProductId`) y `ProductVariant` (`Id`, `PublicId`, `Name`, `Price`, `Stock`,
   `ProductId` — **sin `IsActive`**, D16).
2. `Product` gana `ICollection<ProductImage> Images` y `ICollection<ProductVariant> Variants`
   inicializadas a `new List<>()`; **pierde `ImageUrl`** (A1).
3. Modelos espejo en `Persistence/Models/` heredando de `BaseModel`, con `[Table]`,
   `[Index(nameof(PublicId), IsUnique = true)]` (A22), `[Required]`, `[StringLength]`
   (`Url` 500, `PublicId` 50, `Name` 100). `Models/Product.cs` pierde `ImageUrl` (A20).
4. `TheOfficeDbContext` — dos `DbSet`; en `ConfigureProduct`, `HasConversion<double>()`
   sobre `ProductVariant.Price` (lo exige ARKWS-41; **no** sobre `SortOrder` ni `Stock`),
   con comentario que dé la razón al estilo de `TheOfficeDbContext.cs:27-28`; las dos
   relaciones 1—N con `OnDelete(DeleteBehavior.Cascade)` (A18).
5. `Persistence/Mappers` — mapea las colecciones en ambos sentidos.
6. **`Application/Mappers/ProductMapper.ToDomain(CreateProductRequest, Category)` completo,
   aquí y no más tarde:** el `imageUrl` de v1 se convierte en la única imagen de la galería,
   `IsPrimary = true`, `SortOrder = 0`, `PublicId = $"{request.PublicId}-IMG-1"` (D15).
   Sin esto, `POST /api/v1/products` crea productos sin foto durante cuatro ítems, y sin el
   `PublicId` el segundo POST muere contra el índice único global con un 400.
7. `ToSummary` / `ToResponse` pasan a usar las expresiones de derivación de arriba.
   **Sus formas no cambian.**
8. `Seeders/ProductImageSeeder.cs` — 16 filas con `ProductId` explícito (`HasData` sobre
   una dependiente lo exige, o `OnModelCreating` truena), la **misma URL** de hoy,
   `SortOrder = 0`, `IsPrimary = true`, `PublicId = "PRD-0NN-IMG-1"`, `Id` de la serie
   `b1000000-…` (A21). `ProductSeeder.Build` deja de asignar `ImageUrl`. Ninguno gana
   presentaciones (D3, D19).
9. `dotnet ef migrations add ProductImagesAndVariants -p Infrastructure/TheOffice.Persistence -s Presentation/TheOffice.Api` desde `src/`.
10. **A mano en la migración (D18):** `migrationBuilder.Sql` con un
    `INSERT INTO ProductImages SELECT … FROM Products WHERE Id NOT IN (los 16 sembrados)`,
    colocado **antes** del `DropColumn` — el differ no lo ordena solo.
11. Ajusta lo que deje de compilar: `ProductServiceTests.BuildProduct`.
12. **Verifica:** `make check`; copia de `theoffice.db` + un producto creado por POST +
    `dotnet ef database update` → su `imageUrl` sobrevive; y BD borrada + `make run` →
    `GET /api/v1/products` igual a `scratchpad/v1-baseline-products.csv`.

### Item 2 · ARKWS-42 — repositorio

1. `GetByPublicId` — `.Include(x => x.Images)` y `.Include(x => x.Variants)`.
2. `ProductListItem` (record en `Application/DTOs/`) y `GetPagedList` proyectando
   server-side: la imagen principal con **la misma expresión de orden**, el `Stock`
   agregado y el `VariantCount`. No devuelve entidades de dominio a medio hidratar —
   un `ProductVariant` con `PublicId = null!` es un `NullReferenceException` esperando.
3. El `.Where(x => x.IsActive)` de **producto** no cambia (A4).
4. **Verifica:** `make check`.

### Item 3 · ARKWS-43 — DTOs v2 y la derivación de v1

1. **RED** — pruebas de compatibilidad de v1: suma de stock; producto sin presentaciones
   usa su propio `Stock`; imagen principal marcada; fallback a menor `SortOrder`; sin
   imágenes → cadena vacía. **Son cinco casos, no cuatro.**
2. DTOs: `ProductImageResponse`, `ProductVariantResponse(PublicId, Name, Price, Stock, IsAvailable)`,
   `ProductSummaryV2Response` (forma de D11), `ProductV2Response`.
3. Helpers de derivación compartidos v1/v2 (D12), galería ordenada (`OrderBy(SortOrder)`),
   `IsAvailable = Stock > 0` (A10). Ningún filtrado de presentaciones (D16).
4. `ProductService.GetAllV2` / `GetByPublicIdV2` con las firmas fijadas arriba.
5. **Verifica:** `make check`.

### Item 4 · ARKWS-44 — endpoints de lectura v2

1. `Controllers/ProductV2Controller.cs`, `[ApiVersion("2.0")]`,
   `[Route("api/v{version:apiVersion}/products")]`. `null` → 404, patrón de v1.
   Sin lógica de negocio.
2. `ProductController` no se toca (D1).
3. `TheOffice.Api.http` — peticiones para los endpoints v2 (el repo mantiene una por endpoint).
4. **Verifica:** `make check`; `make run`, los dos documentos en `/scalar`, y
   `GET /api/v1/products` con la misma forma de antes.

### Item 5 · ARKWS-45 — POST v2

1. **RED** — pruebas de las validaciones.
2. `CreateProductV2Request`, `CreateProductImageRequest`, `CreateProductVariantRequest`
   — tres archivos, sin DataAnnotations (A9, A14).
3. `ProductService.CreateV2`, validación a mano con `Result.Failure` (ADR-0003):
   - `images` nulo u omitido = vacío = misma falla ("al menos una imagen").
   - Dos o más `isPrimary` → falla. Ninguna → **se normaliza al persistir**: la de menor
     `sortOrder` queda con `IsPrimary = true` en la fila, no solo derivada al leer.
   - `publicId` de presentación único dentro del request **y contra la base**
     (`FindExistingPublicIds`), comparando `OrdinalIgnoreCase`. Nunca devolver el texto
     del error de SQLite al cliente.
   - Categoría existente por slug (ya lo hace `Create`).
   - Cotas a mano: `url` ≤ 500, `name` ≤ 100, `price` ≥ 0, `stock` ≥ 0. SQLite no
     aplica `VARCHAR(n)` y EF no valida `StringLength` en `SaveChanges`.
   - `PublicId` de imagen `{producto}-IMG-{n}`, asignado una sola vez al crear (D15).
4. `POST` en `ProductV2Controller`, 200 OK (A17). Sin autenticación (A9).
5. **Verifica:** `make check`; y el criterio literal del ticket — `make run`, un POST v2
   con tres imágenes y tres presentaciones, leído de vuelta completo por
   `GET /api/v2/products/{publicId}`.

### Item 6 · ARKWS-47 — pruebas

1. Renombra a inglés los 11 tests existentes.
2. Lo que ARKWS-47 enumera: derivación de v1 (los 5 casos), v2 (galería ordenada,
   `Stock = 0` marcada y **no** filtrada, colección vacía en vez de `null`), creación
   (dos `isPrimary`, `publicId` duplicado, categoría inexistente).
3. Los campos nuevos de `ProductSummaryV2Response` (`Stock`, `IsAvailable`, `VariantCount`)
   — contrato público nuevo sin cobertura si no se agrega.
4. `POST /api/v1/products` convierte su `imageUrl` en la imagen principal — comportamiento
   nuevo que ARKWS-45 pide dejar explícito. **No** se cubre el resto del hueco histórico de
   `Create`: ningún ticket lo pide.
5. **Verifica:** `make check` + `make arch`.

## Cómo se revisó este plan (Step D)

Tres lentes. **Convenciones** encontró que `HasData` sobre una dependiente exige el FK
explícito (el ítem 1 no lo tenía y la migración no habría llegado a generarse), y que la
conversión de `imageUrl` de v1 estaba diferida cuatro ítems de más. **Correctness** encontró
que la imagen que crea v1 no recibía `PublicId` — segundo POST, 400 donde ayer había 200 —,
que listado y detalle podían derivar `imageUrl` distintos sin error, y que la verificación
del backfill ("borra la BD y resiembra") no podía fallar nunca. **Scope** encontró que el
filtro de D13 era inalcanzable y que el plan no entregaba dato con el que Comercial pudiera
validar el punto 3.

Rechazado: renombrar los DTOs a sufijo `…ResponseV2` (ARKWS-43 fija los nombres) y quitar
`HasConversion<double>()` de `ProductVariant.Price` (ARKWS-41 lo exige explícitamente;
se documenta la razón en el código en vez de omitirlo).

## Riesgos

- **La migración es el punto de no retorno.** Dropea `Products.ImageUrl`. El backfill de
  D18 y las 16 filas sembradas tienen que estar bien antes de aplicarla.
- **La derivación de v1 es lo único que sostiene D1**, y vive en la capa con menos
  pruebas: `ProductRepository` no tiene ninguna, y la integración está fuera de alcance
  (ARKWS-47). Mitigación: una sola expresión compartida, y una prueba de que listado y
  detalle coinciden.
- **`GetByPublicId` con dos `Include` de colecciones** produce un producto cartesiano
  (imágenes × presentaciones). Con volúmenes de catálogo es irrelevante, y `AsSplitQuery`
  —la reacción refleja— violaría el criterio de "un solo viaje" de ARKWS-42.
- **Deriva documental:** `docs/data-model.md:86-90` y ADR-0002 quedan desactualizados en
  cuanto esto entre, y los ítems que los arreglan (48–51) están fuera de este run.
- `HasConversion<double>()` en un segundo campo decimal amplía el workaround de ADR-0002.

## Fuera de alcance

- Documentación: ARKWS-48, 49, 50, 51 — otro pase.
- Promociones y precio de descuento (D5).
- Conciliación de existencias con Almacén (D6): `PRD-005` sigue en 25 por D19.
- Carga de los 8–10 productos piloto (D7); y ningún producto lleva presentaciones sembradas.
- Editar y borrar; backoffice; autenticación (D4).
- `WebApplicationFactory` — `Program.cs` no expone `public partial class Program`.
- La moneda de `Price` (A8) — `docs/data-model.md:98` ya lo lleva como TODO, y ahora afecta
  también al precio por presentación.
