using System.Globalization;

using Microsoft.EntityFrameworkCore;

using TheOffice.Persistence.Repositories;

using ApplicationMappers = TheOffice.Application.Mappers;
using DomainEntities = TheOffice.Domain.Entities;
using PersistenceModels = TheOffice.Persistence.Models;

namespace TheOffice.Persistence.Tests.Repositories;

/// <summary>
/// El filtro `IsActive`, el trim del slug, el `LIKE` de la busqueda, el orden alfabetico y la
/// derivacion de la foto principal viven en una expresion que EF Core traduce a SQL: ningun
/// doble las alcanza. Se prueban contra SQLite, que es el motor real de este repositorio.
/// </summary>
public class ProductRepositoryTests : IDisposable
{
  private readonly TestDatabase _database = new();

  private readonly Guid _papeleriaId = Guid.NewGuid();
  private readonly Guid _tecnologiaId = Guid.NewGuid();

  public ProductRepositoryTests()
  {
    _database.Seed(context =>
    {
      context.Categories.Add(BuildCategory(_papeleriaId, "CAT-001", "Papeleria", "papeleria"));
      context.Categories.Add(BuildCategory(_tecnologiaId, "CAT-002", "Tecnologia", "tecnologia"));
    });
  }

  public void Dispose()
  {
    _database.Dispose();
    GC.SuppressFinalize(this);
  }

  // ---------- GetPagedList: filtros ----------

  // El listado publica solo lo activo; el detalle si devuelve inactivos. Son dos contratos.
  [Fact]
  public async Task GetPagedList_InactiveProduct_IsNotListed()
  {
    Seed(BuildProduct("PRD-001", "Resma de papel", _papeleriaId));
    Seed(BuildProduct("PRD-002", "Cuaderno argollado", _papeleriaId, isActive: false));

    var (items, totalItems) = await Sut().GetPagedList(1, 10, null, null);

    Assert.Equal(1, totalItems);
    Assert.Equal(new[] { "PRD-001" }, items.Select(x => x.PublicId));
  }

  [Fact]
  public async Task GetPagedList_CategorySlug_ReturnsOnlyThatCategory()
  {
    Seed(BuildProduct("PRD-001", "Resma de papel", _papeleriaId));
    Seed(BuildProduct("PRD-009", "Monitor 27 pulgadas", _tecnologiaId));

    var (items, totalItems) = await Sut().GetPagedList(1, 10, "tecnologia", null);

    Assert.Equal(1, totalItems);
    Assert.Equal(new[] { "PRD-009" }, items.Select(x => x.PublicId));
  }

  // El slug llega de la URL, y una URL trae espacios con mas frecuencia de la que parece.
  [Fact]
  public async Task GetPagedList_CategorySlugWithSurroundingSpaces_IsTrimmedBeforeFiltering()
  {
    Seed(BuildProduct("PRD-009", "Monitor 27 pulgadas", _tecnologiaId));

    var (items, _) = await Sut().GetPagedList(1, 10, "  tecnologia  ", null);

    Assert.Equal(new[] { "PRD-009" }, items.Select(x => x.PublicId));
  }

  // Un slug en blanco no es un filtro: es la ausencia de filtro.
  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData(null)]
  public async Task GetPagedList_BlankCategorySlug_DoesNotFilter(string? slug)
  {
    Seed(BuildProduct("PRD-001", "Resma de papel", _papeleriaId));
    Seed(BuildProduct("PRD-009", "Monitor 27 pulgadas", _tecnologiaId));

    var (_, totalItems) = await Sut().GetPagedList(1, 10, slug, null);

    Assert.Equal(2, totalItems);
  }

  [Fact]
  public async Task GetPagedList_Search_MatchesTheDescriptionToo()
  {
    Seed(BuildProduct("PRD-001", "Resma de papel", _papeleriaId, description: "500 hojas tamano carta"));
    Seed(BuildProduct("PRD-009", "Monitor 27 pulgadas", _tecnologiaId, description: "Panel IPS"));

    var (items, _) = await Sut().GetPagedList(1, 10, null, "hojas");

    Assert.Equal(new[] { "PRD-001" }, items.Select(x => x.PublicId));
  }

  // `LIKE` de SQLite no distingue mayusculas en ASCII. Se depende de eso: el buscador de la
  // pantalla no normaliza nada antes de mandar el termino.
  [Fact]
  public async Task GetPagedList_SearchInAnotherCase_StillMatches()
  {
    Seed(BuildProduct("PRD-001", "Resma de papel", _papeleriaId));

    var (items, _) = await Sut().GetPagedList(1, 10, null, "RESMA");

    Assert.Equal(new[] { "PRD-001" }, items.Select(x => x.PublicId));
  }

  [Fact]
  public async Task GetPagedList_CategoryAndSearch_AppliesBothFilters()
  {
    Seed(BuildProduct("PRD-001", "Resma de papel", _papeleriaId));
    Seed(BuildProduct("PRD-002", "Resma reciclada", _tecnologiaId));

    var (items, totalItems) = await Sut().GetPagedList(1, 10, "papeleria", "resma");

    Assert.Equal(1, totalItems);
    Assert.Equal(new[] { "PRD-001" }, items.Select(x => x.PublicId));
  }

  // ---------- GetPagedList: orden y paginado ----------

  // Los SKU van a contramano del alfabeto a proposito: si el orden fuera por PublicId — o por
  // el orden de insercion — la asercion seguiria pasando y la prueba no diria nada.
  [Fact]
  public async Task GetPagedList_Always_OrdersByNameNotByInsertionNorBySku()
  {
    Seed(BuildProduct("PRD-001", "Zebra", _papeleriaId));
    Seed(BuildProduct("PRD-003", "Alfa", _papeleriaId));
    Seed(BuildProduct("PRD-002", "Mango", _papeleriaId));

    var (items, _) = await Sut().GetPagedList(1, 10, null, null);

    Assert.Equal(new[] { "Alfa", "Mango", "Zebra" }, items.Select(x => x.Name));
  }

  [Fact]
  public async Task GetPagedList_SecondPage_SkipsTheFirstPageOfTheAlphabeticalOrder()
  {
    Seed(BuildProduct("PRD-001", "Zebra", _papeleriaId));
    Seed(BuildProduct("PRD-002", "Alfa", _papeleriaId));
    Seed(BuildProduct("PRD-003", "Mango", _papeleriaId));

    var (items, _) = await Sut().GetPagedList(2, 2, null, null);

    Assert.Equal(new[] { "Zebra" }, items.Select(x => x.Name));
  }

  // El total es del conjunto filtrado, no de la pagina: la paginacion de la pantalla se calcula
  // con el, y un total igual al tamano de pagina la dejaria siempre en una sola pagina.
  [Fact]
  public async Task GetPagedList_Always_CountsTheWholeFilteredSetNotThePage()
  {
    Seed(BuildProduct("PRD-001", "Alfa", _papeleriaId));
    Seed(BuildProduct("PRD-002", "Beta", _papeleriaId));
    Seed(BuildProduct("PRD-003", "Gama", _papeleriaId));
    Seed(BuildProduct("PRD-009", "Monitor", _tecnologiaId));

    var (items, totalItems) = await Sut().GetPagedList(1, 2, "papeleria", null);

    Assert.Equal(2, items.Count);
    Assert.Equal(3, totalItems);
  }

  [Fact]
  public async Task GetPagedList_PageBeyondTheLast_ReturnsNoItemsButKeepsTheTotal()
  {
    Seed(BuildProduct("PRD-001", "Alfa", _papeleriaId));

    var (items, totalItems) = await Sut().GetPagedList(5, 10, null, null);

    Assert.Empty(items);
    Assert.Equal(1, totalItems);
  }

  // ---------- GetPagedList: derivaciones ----------

  [Fact]
  public async Task GetPagedList_ProductWithVariants_ReportsTheSumOfTheirStock()
  {
    var product = BuildProduct("PRD-010", "Teclado mecanico", _tecnologiaId, stock: 99);
    product.Variants.Add(BuildVariant("PRD-010-NEG", "Negro", 7));
    product.Variants.Add(BuildVariant("PRD-010-BLA", "Blanco", 5));
    Seed(product);

    var (items, _) = await Sut().GetPagedList(1, 10, null, null);

    Assert.Equal(12, items[0].Stock);
    Assert.Equal(2, items[0].VariantCount);
  }

  [Fact]
  public async Task GetPagedList_ProductWithoutVariants_ReportsItsOwnStock()
  {
    Seed(BuildProduct("PRD-001", "Resma de papel", _papeleriaId, stock: 120));

    var (items, _) = await Sut().GetPagedList(1, 10, null, null);

    Assert.Equal(120, items[0].Stock);
    Assert.Equal(0, items[0].VariantCount);
  }

  /// <summary>
  /// La razon de ser de este archivo. El listado deriva la foto principal en SQL y el detalle la
  /// deriva en memoria con <c>ResolvePrimaryImageUrl</c>. Si las dos expresiones divergen, la
  /// misma referencia muestra una foto en la grilla y otra en la ficha, y nada lo reporta.
  /// </summary>
  [Fact]
  public async Task GetPagedList_PrimaryImage_IsTheSameOneTheDetailResolves()
  {
    var product = BuildProduct("PRD-001", "Resma de papel", _papeleriaId);
    product.Images.Add(BuildImage("PRD-001-IMG-1", "https://img/uno.jpg", sortOrder: 0, isPrimary: false));
    product.Images.Add(BuildImage("PRD-001-IMG-2", "https://img/dos.jpg", sortOrder: 5, isPrimary: true));
    product.Images.Add(BuildImage("PRD-001-IMG-3", "https://img/tres.jpg", sortOrder: 1, isPrimary: false));
    Seed(product);

    var (items, _) = await Sut().GetPagedList(1, 10, null, null);
    var detail = await Sut().GetByPublicId("PRD-001");

    Assert.Equal("https://img/dos.jpg", items[0].PrimaryImage?.Url);
    Assert.Equal(items[0].PrimaryImage?.Url, ApplicationMappers.ProductMapper.ResolvePrimaryImageUrl(detail!));
  }

  [Fact]
  public async Task GetPagedList_NoImageMarkedPrimary_FallsBackToTheLowestSortOrder()
  {
    var product = BuildProduct("PRD-001", "Resma de papel", _papeleriaId);
    product.Images.Add(BuildImage("PRD-001-IMG-1", "https://img/uno.jpg", sortOrder: 3, isPrimary: false));
    product.Images.Add(BuildImage("PRD-001-IMG-2", "https://img/dos.jpg", sortOrder: 1, isPrimary: false));
    Seed(product);

    var (items, _) = await Sut().GetPagedList(1, 10, null, null);
    var detail = await Sut().GetByPublicId("PRD-001");

    Assert.Equal("https://img/dos.jpg", items[0].PrimaryImage?.Url);
    Assert.Equal(items[0].PrimaryImage?.Url, ApplicationMappers.ProductMapper.ResolvePrimaryImageUrl(detail!));
  }

  [Fact]
  public async Task GetPagedList_ImagesTiedOnSortOrder_BreaksTheTieByPublicId()
  {
    var product = BuildProduct("PRD-001", "Resma de papel", _papeleriaId);
    product.Images.Add(BuildImage("PRD-001-IMG-2", "https://img/dos.jpg", sortOrder: 0, isPrimary: false));
    product.Images.Add(BuildImage("PRD-001-IMG-1", "https://img/uno.jpg", sortOrder: 0, isPrimary: false));
    Seed(product);

    var (items, _) = await Sut().GetPagedList(1, 10, null, null);
    var detail = await Sut().GetByPublicId("PRD-001");

    Assert.Equal("https://img/uno.jpg", items[0].PrimaryImage?.Url);
    Assert.Equal(items[0].PrimaryImage?.Url, ApplicationMappers.ProductMapper.ResolvePrimaryImageUrl(detail!));
  }

  [Fact]
  public async Task GetPagedList_ProductWithoutImages_ReturnsNoPrimaryImage()
  {
    Seed(BuildProduct("PRD-001", "Resma de papel", _papeleriaId));

    var (items, _) = await Sut().GetPagedList(1, 10, null, null);

    Assert.Null(items[0].PrimaryImage);
  }

  [Fact]
  public async Task GetPagedList_Always_CarriesTheCategoryNameAndSlug()
  {
    Seed(BuildProduct("PRD-001", "Resma de papel", _papeleriaId));

    var (items, _) = await Sut().GetPagedList(1, 10, null, null);

    Assert.Equal("Papeleria", items[0].CategoryName);
    Assert.Equal("papeleria", items[0].CategorySlug);
  }

  // ---------- GetByPublicId ----------

  [Fact]
  public async Task GetByPublicId_Always_BringsCategoryImagesAndVariants()
  {
    var product = BuildProduct("PRD-010", "Teclado mecanico", _tecnologiaId);
    product.Images.Add(BuildImage("PRD-010-IMG-1", "https://img/teclado.jpg", sortOrder: 0, isPrimary: true));
    product.Variants.Add(BuildVariant("PRD-010-NEG", "Negro", 7));
    Seed(product);

    var detail = await Sut().GetByPublicId("PRD-010");

    Assert.NotNull(detail);
    Assert.Equal("Tecnologia", detail.Category?.Name);
    Assert.Single(detail.Images);
    Assert.Single(detail.Variants);
  }

  // El detalle no filtra por `IsActive`: la ficha de un descontinuado sigue siendo alcanzable.
  [Fact]
  public async Task GetByPublicId_InactiveProduct_IsStillReturned()
  {
    Seed(BuildProduct("PRD-002", "Cuaderno argollado", _papeleriaId, isActive: false));

    var detail = await Sut().GetByPublicId("PRD-002");

    Assert.NotNull(detail);
    Assert.False(detail.IsActive);
  }

  [Fact]
  public async Task GetByPublicId_UnknownPublicId_ReturnsNull()
  {
    Seed(BuildProduct("PRD-001", "Resma de papel", _papeleriaId));

    var detail = await Sut().GetByPublicId("PRD-999");

    Assert.Null(detail);
  }

  // ---------- Create ----------

  /// <summary>
  /// `Price` viaja a la base como `double` por el workaround de SQLite (ADR-0002). Un precio de
  /// catalogo tiene que volver identico; el dia que la conversion se retire, esto sigue verde.
  /// </summary>
  [Theory]
  [InlineData("18900")]
  [InlineData("18900.50")]
  [InlineData("1250000.99")]
  public async Task Create_Always_RoundTripsThePriceThroughTheDoubleConversion(string raw)
  {
    var price = decimal.Parse(raw, CultureInfo.InvariantCulture);

    var result = await Sut().Create(BuildDomainProduct("PRD-100", "Producto nuevo", price));

    Assert.True(result.IsSuccess);
    var stored = await Sut().GetByPublicId("PRD-100");
    Assert.Equal(price, stored!.Price);
  }

  // El repositorio muta la entidad recibida: le escribe de vuelta el Id que genero la base.
  [Fact]
  public async Task Create_Always_WritesTheStoredIdBackOnTheDomainEntity()
  {
    var product = BuildDomainProduct("PRD-100", "Producto nuevo", 18900m);

    await Sut().Create(product);

    Assert.NotEqual(Guid.Empty, product.Id);
    using var context = _database.NewContext();
    Assert.True(await context.Products.AnyAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken));
  }

  // El indice unico de PublicId revienta dentro del repositorio y sale como Result.Failure:
  // el mensaje de SQLite no debe escaparse como excepcion hasta el controller.
  [Fact]
  public async Task Create_DuplicatedPublicId_ReturnsFailureInsteadOfThrowing()
  {
    Seed(BuildProduct("PRD-001", "Resma de papel", _papeleriaId));

    var result = await Sut().Create(BuildDomainProduct("PRD-001", "Resma repetida", 18900m));

    Assert.False(result.IsSuccess);
    Assert.StartsWith("Failed to create product:", result.Error, StringComparison.Ordinal);
  }

  [Fact]
  public async Task Create_ProductWithImagesAndVariants_PersistsTheWholeGraph()
  {
    var product = BuildDomainProduct("PRD-100", "Producto nuevo", 18900m);
    product.Images.Add(new DomainEntities.ProductImage
    {
      PublicId = "PRD-100-IMG-1",
      Url = "https://img/nuevo.jpg",
      SortOrder = 0,
      IsPrimary = true
    });
    product.Variants.Add(new DomainEntities.ProductVariant
    {
      PublicId = "PRD-100-NEG",
      Name = "Negro",
      Price = 18900m,
      Stock = 4
    });

    await Sut().Create(product);

    var stored = await Sut().GetByPublicId("PRD-100");
    Assert.Single(stored!.Images);
    Assert.Single(stored.Variants);
  }

  // ---------- FindExistingPublicIds ----------

  [Fact]
  public async Task FindExistingPublicIds_TakenProductAndVariant_ReturnsBoth()
  {
    var product = BuildProduct("PRD-010", "Teclado mecanico", _tecnologiaId);
    product.Variants.Add(BuildVariant("PRD-010-NEG", "Negro", 7));
    Seed(product);

    var taken = await Sut().FindExistingPublicIds("PRD-010", ["PRD-010-NEG", "PRD-010-AZU"]);

    Assert.Equal(new[] { "PRD-010", "PRD-010-NEG" }, taken);
  }

  [Fact]
  public async Task FindExistingPublicIds_NothingTaken_ReturnsEmpty()
  {
    Seed(BuildProduct("PRD-010", "Teclado mecanico", _tecnologiaId));

    var taken = await Sut().FindExistingPublicIds("PRD-100", ["PRD-100-NEG"]);

    Assert.Empty(taken);
  }

  [Fact]
  public async Task FindExistingPublicIds_NoVariants_OnlyChecksTheProduct()
  {
    Seed(BuildProduct("PRD-010", "Teclado mecanico", _tecnologiaId));

    var taken = await Sut().FindExistingPublicIds("PRD-010", []);

    Assert.Equal(new[] { "PRD-010" }, taken);
  }

  // ---------- Helpers ----------

  private ProductRepository Sut() => new(_database.NewContext());

  private void Seed(PersistenceModels.Product product)
  {
    _database.Seed(context => context.Products.Add(product));
  }

  private static PersistenceModels.Category BuildCategory(Guid id, string publicId, string name, string slug)
  {
    return new PersistenceModels.Category
    {
      Id = id,
      PublicId = publicId,
      Name = name,
      Slug = slug,
      Description = $"Categoria {name}"
    };
  }

  private static PersistenceModels.Product BuildProduct(
    string publicId,
    string name,
    Guid categoryId,
    bool isActive = true,
    int stock = 10,
    string description = "Descripcion de catalogo")
  {
    return new PersistenceModels.Product
    {
      Id = Guid.NewGuid(),
      PublicId = publicId,
      Name = name,
      Description = description,
      Price = 18900m,
      Stock = stock,
      IsActive = isActive,
      CategoryId = categoryId
    };
  }

  private static PersistenceModels.ProductImage BuildImage(string publicId, string url, int sortOrder, bool isPrimary)
  {
    return new PersistenceModels.ProductImage
    {
      Id = Guid.NewGuid(),
      PublicId = publicId,
      Url = url,
      SortOrder = sortOrder,
      IsPrimary = isPrimary
    };
  }

  private static PersistenceModels.ProductVariant BuildVariant(string publicId, string name, int stock)
  {
    return new PersistenceModels.ProductVariant
    {
      Id = Guid.NewGuid(),
      PublicId = publicId,
      Name = name,
      Price = 18900m,
      Stock = stock
    };
  }

  private DomainEntities.Product BuildDomainProduct(string publicId, string name, decimal price)
  {
    return new DomainEntities.Product
    {
      Id = Guid.NewGuid(),
      PublicId = publicId,
      Name = name,
      Description = "Descripcion de catalogo",
      Price = price,
      Stock = 10,
      IsActive = true,
      CategoryId = _papeleriaId
    };
  }
}
