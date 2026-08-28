using NSubstitute;

using TheOffice.Application.DTOs;
using TheOffice.Application.Interfaces.Adapters;
using TheOffice.Application.Interfaces.Persistence;
using TheOffice.Application.Services;
using TheOffice.Domain.Common;
using TheOffice.Domain.Entities;

namespace TheOffice.Application.Tests.Services;

public class ProductServiceTests
{
  private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
  private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
  private readonly INotificationAdapter _notificationAdapter = Substitute.For<INotificationAdapter>();
  private readonly ProductService _sut;

  public ProductServiceTests()
  {
    _sut = new ProductService(_productRepository, _categoryRepository, _notificationAdapter);
  }

  // ---------- GetAll: buscar todos ----------

  [Fact]
  public async Task GetAll_NoFilters_ReturnsPageWithAllProducts()
  {
    var sillas = BuildCategory("CAT-001", "Sillas", "sillas");
    var products = new List<ProductListItem>
    {
      BuildListItem("PRD-001", "Silla Ergonomica", 199.99m, 10, sillas),
      BuildListItem("PRD-002", "Silla Gamer", 349.50m, 4, sillas)
    };
    _productRepository.GetPagedList(1, 6, null, null).Returns(((IReadOnlyList<ProductListItem>)products, 2));

    var result = await _sut.GetAll(new ProductQuery());

    await _productRepository.Received(1).GetPagedList(1, 6, null, null);
    Assert.Equal(1, result.Page);
    Assert.Equal(6, result.PageSize);
    Assert.Equal(2, result.TotalItems);
    Assert.Equal(1, result.TotalPages);
    Assert.Equal(
      new[]
      {
        new ProductSummaryResponse("PRD-001", "Silla Ergonomica", 199.99m, "https://img/PRD-001.jpg", 10, "Sillas", "sillas"),
        new ProductSummaryResponse("PRD-002", "Silla Gamer", 349.50m, "https://img/PRD-002.jpg", 4, "Sillas", "sillas")
      },
      result.Items);
  }

  [Fact]
  public async Task GetAll_EmptyRepository_ReturnsEmptyPage()
  {
    _productRepository.GetPagedList(1, 6, null, null).Returns(((IReadOnlyList<ProductListItem>)[], 0));

    var result = await _sut.GetAll(new ProductQuery());

    Assert.Empty(result.Items);
    Assert.Equal(0, result.TotalItems);
    Assert.Equal(0, result.TotalPages);
  }

  [Fact]
  public async Task GetAll_ProductWithoutCategory_ReturnsEmptyNameAndSlug()
  {
    var products = new List<ProductListItem> { BuildListItem("PRD-003", "Mesa Suelta", 89m, 1, category: null) };
    _productRepository.GetPagedList(1, 6, null, null).Returns(((IReadOnlyList<ProductListItem>)products, 1));

    var result = await _sut.GetAll(new ProductQuery());

    var item = Assert.Single(result.Items);
    Assert.Equal(string.Empty, item.CategoryName);
    Assert.Equal(string.Empty, item.CategorySlug);
  }

  // ---------- GetAll: por categoria ----------

  [Fact]
  public async Task GetAll_WithCategory_PassesSlugToRepository()
  {
    _productRepository.GetPagedList(1, 6, "sillas", null).Returns(((IReadOnlyList<ProductListItem>)[], 0));

    await _sut.GetAll(new ProductQuery { Category = "sillas" });

    await _productRepository.Received(1).GetPagedList(1, 6, "sillas", null);
  }

  [Fact]
  public async Task GetAll_WithCategory_ReturnsOnlyWhatRepositoryProvides()
  {
    var mesas = BuildCategory("CAT-002", "Mesas", "mesas");
    var products = new List<ProductListItem> { BuildListItem("PRD-010", "Mesa de Juntas", 899m, 2, mesas) };
    _productRepository.GetPagedList(1, 6, "mesas", null).Returns(((IReadOnlyList<ProductListItem>)products, 1));

    var result = await _sut.GetAll(new ProductQuery { Category = "mesas" });

    var item = Assert.Single(result.Items);
    Assert.Equal("PRD-010", item.PublicId);
    Assert.Equal("mesas", item.CategorySlug);
    Assert.Equal(1, result.TotalItems);
  }

  [Fact]
  public async Task GetAll_WithCategoryAndSearch_PassesBothFilters()
  {
    _productRepository.GetPagedList(2, 10, "sillas", "gamer").Returns(((IReadOnlyList<ProductListItem>)[], 0));

    await _sut.GetAll(new ProductQuery { Page = 2, PageSize = 10, Category = "sillas", Search = "gamer" });

    await _productRepository.Received(1).GetPagedList(2, 10, "sillas", "gamer");
  }

  // ---------- GetAll: normalizacion de paginacion ----------

  [Theory]
  [InlineData(0, 6, 1, 6)]     // page < 1 -> 1
  [InlineData(-5, 6, 1, 6)]    // page negativo -> 1
  [InlineData(2, 6, 2, 6)]     // valores validos se respetan
  [InlineData(1, 0, 1, 1)]     // pageSize < 1 -> 1
  [InlineData(1, 100, 1, 50)]  // pageSize > MaxPageSize -> 50
  [InlineData(1, 50, 1, 50)]   // limite exacto
  public async Task GetAll_NormalizesPaging(int page, int pageSize, int expectedPage, int expectedPageSize)
  {
    _productRepository.GetPagedList(expectedPage, expectedPageSize, null, null)
      .Returns(((IReadOnlyList<ProductListItem>)[], 0));

    var result = await _sut.GetAll(new ProductQuery { Page = page, PageSize = pageSize });

    await _productRepository.Received(1).GetPagedList(expectedPage, expectedPageSize, null, null);
    Assert.Equal(expectedPage, result.Page);
    Assert.Equal(expectedPageSize, result.PageSize);
  }

  // ---------- GetByPublicId ----------

  [Fact]
  public async Task GetByPublicId_ExistingProduct_ReturnsSuccessResult()
  {
    var sillas = BuildCategory("CAT-001", "Sillas", "sillas");
    _productRepository.GetByPublicId("PRD-001").Returns(BuildProduct("PRD-001", "Silla Ergonomica", 199.99m, 10, sillas));

    var result = await _sut.GetByPublicId("PRD-001");

    Assert.NotNull(result);
    Assert.True(result.IsSuccess);
    Assert.Null(result.Error);
    Assert.Equal(
      new ProductResponse(
        "PRD-001",
        "Silla Ergonomica",
        "Descripcion de Silla Ergonomica",
        199.99m,
        "https://img/PRD-001.jpg",
        10,
        true,
        new CategoryResponse("CAT-001", "Sillas", "sillas", "Descripcion de Sillas")),
      result.Value);
  }

  [Fact]
  public async Task GetByPublicId_ProductWithoutCategory_ReturnsNullCategory()
  {
    _productRepository.GetByPublicId("PRD-003").Returns(BuildProduct("PRD-003", "Mesa Suelta", 89m, 1, category: null));

    var result = await _sut.GetByPublicId("PRD-003");

    Assert.NotNull(result);
    Assert.True(result.IsSuccess);
    Assert.Null(result.Value!.Category);
  }

  [Fact]
  public async Task GetByPublicId_MissingProduct_ReturnsNull()
  {
    _productRepository.GetByPublicId("PRD-999").Returns((Product?)null);

    var result = await _sut.GetByPublicId("PRD-999");

    // El service devuelve null (no un Result.Failure); el controller lo traduce a 404.
    Assert.Null(result);
  }

  [Fact]
  public async Task GetByPublicId_PassesPublicIdToRepository()
  {
    _productRepository.GetByPublicId("PRD-001").Returns((Product?)null);

    await _sut.GetByPublicId("PRD-001");

    await _productRepository.Received(1).GetByPublicId("PRD-001");
  }

  // ---------- Derivacion de v1: el contrato congelado ----------

  [Fact]
  public async Task GetByPublicId_ProductWithVariants_SumsVariantStock()
  {
    var product = BuildProduct("PRD-005", "Silla Ergonomica", 689000m, 25, BuildCategory("CAT-002", "Mobiliario", "mobiliario"));
    product.Variants.Add(BuildVariant(product.Id, "PRD-005-NEG", "Negro", 689000m, 8));
    product.Variants.Add(BuildVariant(product.Id, "PRD-005-GRI", "Gris", 689000m, 3));
    product.Variants.Add(BuildVariant(product.Id, "PRD-005-ROJ", "Rojo", 689000m, 0));
    _productRepository.GetByPublicId("PRD-005").Returns(product);

    var result = await _sut.GetByPublicId("PRD-005");

    // 8 + 3 + 0, no el Stock 25 que el producto lleva encima.
    Assert.Equal(11, result!.Value!.Stock);
  }

  [Fact]
  public async Task GetByPublicId_ProductWithoutVariants_UsesOwnStock()
  {
    var product = BuildProduct("PRD-001", "Resma de papel", 18900m, 120, category: null);
    _productRepository.GetByPublicId("PRD-001").Returns(product);

    var result = await _sut.GetByPublicId("PRD-001");

    Assert.Equal(120, result!.Value!.Stock);
  }

  [Fact]
  public async Task GetByPublicId_WithPrimaryImage_PrefersItOverSortOrder()
  {
    var product = BuildProduct("PRD-004", "Marcador borrable", 15400m, 10, category: null);
    product.Images.Clear();
    product.Images.Add(BuildImage(product.Id, "PRD-004", "https://img/no-principal.jpg", 0, false));
    product.Images.Add(BuildImage(product.Id, "PRD-004", "https://img/principal.jpg", 9, true));
    _productRepository.GetByPublicId("PRD-004").Returns(product);

    var result = await _sut.GetByPublicId("PRD-004");

    Assert.Equal("https://img/principal.jpg", result!.Value!.ImageUrl);
  }

  [Fact]
  public async Task GetByPublicId_WithoutPrimaryImage_ReturnsLowestSortOrderUrl()
  {
    var product = BuildProduct("PRD-002", "Cuaderno argollado", 12500m, 10, category: null);
    product.Images.Clear();
    product.Images.Add(BuildImage(product.Id, "PRD-002", "https://img/segunda.jpg", 5, false));
    product.Images.Add(BuildImage(product.Id, "PRD-002", "https://img/primera.jpg", 1, false));
    _productRepository.GetByPublicId("PRD-002").Returns(product);

    var result = await _sut.GetByPublicId("PRD-002");

    Assert.Equal("https://img/primera.jpg", result!.Value!.ImageUrl);
  }

  [Fact]
  public async Task GetByPublicId_ProductWithoutImages_ReturnsEmptyImageUrl()
  {
    var product = BuildProduct("PRD-003", "Boligrafo tinta negra", 9800m, 10, category: null);
    product.Images.Clear();
    _productRepository.GetByPublicId("PRD-003").Returns(product);

    var result = await _sut.GetByPublicId("PRD-003");

    Assert.Equal(string.Empty, result!.Value!.ImageUrl);
  }

  // ---------- v2: galeria y presentaciones ----------

  [Fact]
  public async Task GetByPublicIdV2_ReturnsGalleryOrderedBySortOrder()
  {
    var product = BuildProduct("PRD-005", "Silla Ergonomica", 689000m, 25, category: null);
    product.Images.Clear();
    product.Images.Add(BuildImage(product.Id, "PRD-005", "https://img/tercera.jpg", 2, false));
    product.Images.Add(BuildImage(product.Id, "PRD-005", "https://img/primera.jpg", 0, true));
    product.Images.Add(BuildImage(product.Id, "PRD-005", "https://img/segunda.jpg", 1, false));
    _productRepository.GetByPublicId("PRD-005").Returns(product);

    var result = await _sut.GetByPublicIdV2("PRD-005");

    Assert.Equal(
      new[] { "https://img/primera.jpg", "https://img/segunda.jpg", "https://img/tercera.jpg" },
      result!.Value!.Images.Select(x => x.Url));
  }

  [Fact]
  public async Task GetByPublicIdV2_SoldOutVariant_IsReturnedMarkedUnavailable()
  {
    var product = BuildProduct("PRD-005", "Silla Ergonomica", 689000m, 25, category: null);
    product.Variants.Add(BuildVariant(product.Id, "PRD-005-NEG", "Negro", 689000m, 8));
    product.Variants.Add(BuildVariant(product.Id, "PRD-005-ROJ", "Rojo", 689000m, 0));
    _productRepository.GetByPublicId("PRD-005").Returns(product);

    var result = await _sut.GetByPublicIdV2("PRD-005");

    // La agotada no se filtra: el cliente necesita saber que ese color existe.
    Assert.Equal(2, result!.Value!.Variants.Count);
    var soldOut = result.Value.Variants.Single(x => x.PublicId == "PRD-005-ROJ");
    Assert.Equal(0, soldOut.Stock);
    Assert.False(soldOut.IsAvailable);
    Assert.True(result.Value.Variants.Single(x => x.PublicId == "PRD-005-NEG").IsAvailable);
  }

  [Fact]
  public async Task GetByPublicIdV2_ProductWithoutVariants_ReturnsEmptyCollection()
  {
    var product = BuildProduct("PRD-001", "Resma de papel", 18900m, 120, category: null);
    _productRepository.GetByPublicId("PRD-001").Returns(product);

    var result = await _sut.GetByPublicIdV2("PRD-001");

    Assert.NotNull(result!.Value!.Variants);
    Assert.Empty(result.Value.Variants);
  }

  [Fact]
  public async Task GetByPublicIdV2_MissingProduct_ReturnsNull()
  {
    _productRepository.GetByPublicId("PRD-999").Returns((Product?)null);

    var result = await _sut.GetByPublicIdV2("PRD-999");

    Assert.Null(result);
  }

  [Fact]
  public async Task GetAllV2_MapsPrimaryImageAvailabilityAndVariantCount()
  {
    var items = new List<ProductListItem>
    {
      new("PRD-005", "Silla Ergonomica", 689000m, 11, "Mobiliario", "mobiliario",
        new ProductImageResponse("PRD-005-IMG-1", "https://img/PRD-005.jpg", 0, true), 3),
      new("PRD-006", "Escritorio en L", 899000m, 0, "Mobiliario", "mobiliario", null, 0)
    };
    _productRepository.GetPagedList(1, 6, null, null).Returns(((IReadOnlyList<ProductListItem>)items, 2));

    var result = await _sut.GetAllV2(new ProductQuery());

    Assert.Equal(
      new[]
      {
        new ProductSummaryV2Response("PRD-005", "Silla Ergonomica", 689000m,
          new ProductImageResponse("PRD-005-IMG-1", "https://img/PRD-005.jpg", 0, true), 11, true, 3, "Mobiliario", "mobiliario"),
        new ProductSummaryV2Response("PRD-006", "Escritorio en L", 899000m, null, 0, false, 0, "Mobiliario", "mobiliario")
      },
      result.Items);
  }

  // ---------- Creacion ----------

  [Fact]
  public async Task CreateV2_NoImages_ReturnsFailure()
  {
    var result = await _sut.CreateV2(BuildCreateV2Request(images: []));

    Assert.False(result.IsSuccess);
    Assert.Equal("A product needs at least one image", result.Error);
  }

  [Fact]
  public async Task CreateV2_TwoPrimaryImages_ReturnsFailure()
  {
    var request = BuildCreateV2Request(images:
    [
      new CreateProductImageRequest("https://img/a.jpg", 0, true),
      new CreateProductImageRequest("https://img/b.jpg", 1, true)
    ]);

    var result = await _sut.CreateV2(request);

    Assert.False(result.IsSuccess);
    Assert.Equal("Only one image can be marked as primary", result.Error);
  }

  [Fact]
  public async Task CreateV2_DuplicatedVariantPublicId_ReturnsFailure()
  {
    var request = BuildCreateV2Request(variants:
    [
      new CreateProductVariantRequest("PRD-018-NEG", "Negro", 100m, 1),
      new CreateProductVariantRequest("prd-018-neg", "Negro otra vez", 100m, 1)
    ]);

    var result = await _sut.CreateV2(request);

    Assert.False(result.IsSuccess);
    Assert.Equal("Duplicated variant public id: PRD-018-NEG", result.Error);
  }

  [Fact]
  public async Task CreateV2_MissingCategory_ReturnsFailure()
  {
    _categoryRepository.GetBySlug("no-existe").Returns((Category?)null);

    var result = await _sut.CreateV2(BuildCreateV2Request(categorySlug: "no-existe"));

    Assert.False(result.IsSuccess);
    Assert.Equal("Category not found: no-existe", result.Error);
  }

  [Fact]
  public async Task CreateV2_PublicIdAlreadyTaken_ReturnsFailure()
  {
    StubSuccessfulCreate();
    _productRepository.FindExistingPublicIds(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
      .Returns((IReadOnlyList<string>)["PRD-018"]);

    var result = await _sut.CreateV2(BuildCreateV2Request());

    Assert.False(result.IsSuccess);
    Assert.Equal("Public id already in use: PRD-018", result.Error);
    // No se llega a guardar: la unicidad se resuelve antes de tocar la base.
    await _productRepository.DidNotReceive().Create(Arg.Any<Product>());
  }

  [Fact]
  public async Task CreateV2_WithoutPrimaryImage_MarksLowestSortOrderAsPrimary()
  {
    StubSuccessfulCreate();
    Product? saved = null;
    _productRepository.Create(Arg.Do<Product>(x => saved = x)).Returns(Result.Success());

    var request = BuildCreateV2Request(images:
    [
      new CreateProductImageRequest("https://img/segunda.jpg", 9, false),
      new CreateProductImageRequest("https://img/primera.jpg", 2, false)
    ]);

    var result = await _sut.CreateV2(request);

    Assert.True(result.IsSuccess);
    // La marca queda en la fila, no solo derivada al leer.
    var primary = Assert.Single(saved!.Images, x => x.IsPrimary);
    Assert.Equal("https://img/primera.jpg", primary.Url);
    Assert.Equal("PRD-018-IMG-1", primary.PublicId);
  }

  [Fact]
  public async Task Create_V1Request_TurnsImageUrlIntoPrimaryGalleryImage()
  {
    _categoryRepository.GetBySlug("papeleria").Returns(BuildCategory("CAT-001", "Papeleria", "papeleria"));
    Product? saved = null;
    _productRepository.Create(Arg.Do<Product>(x => saved = x)).Returns(Result.Success());

    var result = await _sut.Create(new CreateProductRequest(
      "PRD-020", "Resma", "Descripcion", 1000m, "https://media/prd-020.png", 5, "papeleria"));

    Assert.True(result.IsSuccess);
    var image = Assert.Single(saved!.Images);
    Assert.Equal("PRD-020-IMG-1", image.PublicId);
    Assert.Equal("https://media/prd-020.png", image.Url);
    Assert.Equal(0, image.SortOrder);
    Assert.True(image.IsPrimary);
    // El contrato de v1 no cambia: la respuesta trae la misma imageUrl que se mando.
    Assert.Equal("https://media/prd-020.png", result.Value!.ImageUrl);
  }

  [Fact]
  public async Task CreateV2_EmptyPublicId_ReturnsFailure()
  {
    var result = await _sut.CreateV2(BuildCreateV2Request(publicId: "  "));

    Assert.False(result.IsSuccess);
    Assert.Equal("Public id is required and cannot exceed 40 characters", result.Error);
  }

  [Fact]
  public async Task CreateV2_PublicIdTooLongForDerivedImageId_ReturnsFailure()
  {
    // 41 caracteres: el {publicId}-IMG-1 derivado se pasaria de los 50 de la columna.
    var result = await _sut.CreateV2(BuildCreateV2Request(publicId: new string('P', 41)));

    Assert.False(result.IsSuccess);
    Assert.Equal("Public id is required and cannot exceed 40 characters", result.Error);
  }

  [Fact]
  public async Task CreateV2_EmptyName_ReturnsFailure()
  {
    var result = await _sut.CreateV2(BuildCreateV2Request(name: ""));

    Assert.False(result.IsSuccess);
    Assert.Equal("Name is required and cannot exceed 150 characters", result.Error);
  }

  [Fact]
  public async Task CreateV2_DescriptionTooLong_ReturnsFailure()
  {
    var result = await _sut.CreateV2(BuildCreateV2Request(description: new string('D', 1001)));

    Assert.False(result.IsSuccess);
    Assert.Equal("Description cannot exceed 1000 characters", result.Error);
  }

  [Fact]
  public async Task CreateV2_VariantPublicIdTooLong_ReturnsFailure()
  {
    var request = BuildCreateV2Request(variants:
    [
      new CreateProductVariantRequest(new string('V', 51), "Negro", 100m, 1)
    ]);

    var result = await _sut.CreateV2(request);

    Assert.False(result.IsSuccess);
    Assert.Equal("Variant public id is required and cannot exceed 50 characters", result.Error);
  }

  [Fact]
  public async Task CreateV2_TotalVariantStockOverflowsInt_ReturnsFailure()
  {
    // Cada Stock es valido por separado; la suma no cabe en el int que expone v1.
    var request = BuildCreateV2Request(variants:
    [
      new CreateProductVariantRequest("PRD-018-NEG", "Negro", 100m, int.MaxValue),
      new CreateProductVariantRequest("PRD-018-GRI", "Gris", 100m, 1)
    ]);

    var result = await _sut.CreateV2(request);

    Assert.False(result.IsSuccess);
    Assert.Equal("Total stock across variants cannot exceed 2147483647", result.Error);
  }

  // ---------- Helpers ----------

  private static Category BuildCategory(string publicId, string name, string slug)
  {
    return new Category
    {
      Id = Guid.NewGuid(),
      PublicId = publicId,
      Name = name,
      Slug = slug,
      Description = $"Descripcion de {name}"
    };
  }

  private static Product BuildProduct(string publicId, string name, decimal price, int stock, Category? category)
  {
    var productId = Guid.NewGuid();

    var product = new Product
    {
      Id = productId,
      PublicId = publicId,
      Name = name,
      Description = $"Descripcion de {name}",
      Price = price,
      Stock = stock,
      IsActive = true,
      CategoryId = category?.Id ?? Guid.Empty,
      Category = category
    };

    product.Images.Add(BuildImage(productId, publicId, $"https://img/{publicId}.jpg", 0, true));

    return product;
  }

  private static ProductImage BuildImage(Guid productId, string productPublicId, string url, int sortOrder, bool isPrimary)
  {
    return new ProductImage
    {
      Id = Guid.NewGuid(),
      PublicId = $"{productPublicId}-IMG-{sortOrder + 1}",
      Url = url,
      SortOrder = sortOrder,
      IsPrimary = isPrimary,
      ProductId = productId
    };
  }

  private static ProductListItem BuildListItem(string publicId, string name, decimal price, int stock, Category? category)
  {
    return new ProductListItem(
      publicId,
      name,
      price,
      stock,
      category?.Name ?? string.Empty,
      category?.Slug ?? string.Empty,
      new ProductImageResponse($"{publicId}-IMG-1", $"https://img/{publicId}.jpg", 0, true),
      0);
  }

  private static ProductVariant BuildVariant(Guid productId, string publicId, string name, decimal price, int stock)
  {
    return new ProductVariant
    {
      Id = Guid.NewGuid(),
      PublicId = publicId,
      Name = name,
      Price = price,
      Stock = stock,
      ProductId = productId
    };
  }

  private void StubSuccessfulCreate()
  {
    _categoryRepository.GetBySlug("mobiliario").Returns(BuildCategory("CAT-002", "Mobiliario", "mobiliario"));
    _productRepository.FindExistingPublicIds(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
      .Returns((IReadOnlyList<string>)[]);
    _productRepository.Create(Arg.Any<Product>()).Returns(Result.Success());
  }

  private static CreateProductV2Request BuildCreateV2Request(
    string categorySlug = "mobiliario",
    string publicId = "PRD-018",
    string name = "Silla ejecutiva",
    string description = "Silla ejecutiva reclinable.",
    IReadOnlyList<CreateProductImageRequest>? images = null,
    IReadOnlyList<CreateProductVariantRequest>? variants = null)
  {
    return new CreateProductV2Request(
      publicId,
      name,
      description,
      749000m,
      0,
      categorySlug,
      images ?? [new CreateProductImageRequest("https://img/frente.jpg", 0, true)],
      variants ?? []);
  }
}
