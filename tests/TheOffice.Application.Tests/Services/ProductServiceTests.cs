using NSubstitute;

using TheOffice.Application.DTOs;
using TheOffice.Application.Interfaces.Adapters;
using TheOffice.Application.Interfaces.Persistence;
using TheOffice.Application.Services;
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
  public async Task GetAll_SinFiltros_DevuelvePaginaConTodosLosProductos()
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
  public async Task GetAll_RepositorioSinResultados_DevuelvePaginaVacia()
  {
    _productRepository.GetPagedList(1, 6, null, null).Returns(((IReadOnlyList<ProductListItem>)[], 0));

    var result = await _sut.GetAll(new ProductQuery());

    Assert.Empty(result.Items);
    Assert.Equal(0, result.TotalItems);
    Assert.Equal(0, result.TotalPages);
  }

  [Fact]
  public async Task GetAll_ProductoSinCategoria_DevuelveNombreYSlugVacios()
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
  public async Task GetAll_ConCategoria_PropagaElSlugAlRepositorio()
  {
    _productRepository.GetPagedList(1, 6, "sillas", null).Returns(((IReadOnlyList<ProductListItem>)[], 0));

    await _sut.GetAll(new ProductQuery { Category = "sillas" });

    await _productRepository.Received(1).GetPagedList(1, 6, "sillas", null);
  }

  [Fact]
  public async Task GetAll_ConCategoria_DevuelveSoloLoQueEntregaElRepositorio()
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
  public async Task GetAll_ConCategoriaYBusqueda_PropagaAmbosFiltros()
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
  public async Task GetAll_NormalizaPaginacion(int page, int pageSize, int expectedPage, int expectedPageSize)
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
  public async Task GetByPublicId_ProductoExistente_DevuelveResultExitoso()
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
  public async Task GetByPublicId_ProductoSinCategoria_DevuelveCategoriaNula()
  {
    _productRepository.GetByPublicId("PRD-003").Returns(BuildProduct("PRD-003", "Mesa Suelta", 89m, 1, category: null));

    var result = await _sut.GetByPublicId("PRD-003");

    Assert.NotNull(result);
    Assert.True(result.IsSuccess);
    Assert.Null(result.Value!.Category);
  }

  [Fact]
  public async Task GetByPublicId_ProductoInexistente_DevuelveNull()
  {
    _productRepository.GetByPublicId("PRD-999").Returns((Product?)null);

    var result = await _sut.GetByPublicId("PRD-999");

    // El service devuelve null (no un Result.Failure); el controller lo traduce a 404.
    Assert.Null(result);
  }

  [Fact]
  public async Task GetByPublicId_PropagaElPublicIdAlRepositorio()
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
}
