using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using TheOffice.Application.DTOs;

namespace TheOffice.Api.Tests.Controllers;

/// <summary>
/// El contrato HTTP del catalogo: lo que el frontend Angular espeja en `catalog.models.ts`.
/// Aqui se prueba lo que las unitarias de `ProductService` no alcanzan — el enlace del query
/// string, los codigos de estado y los nombres de las propiedades del JSON.
/// </summary>
public class ProductControllerTests : IClassFixture<ApiFactory>
{
  private readonly HttpClient _client;

  public ProductControllerTests(ApiFactory factory)
  {
    _client = factory.CreateClient();
  }

  // ---------- GET /products ----------

  // 6, no 10: el 10 es una decision del frontend que viaja explicita en cada peticion.
  [Fact]
  public async Task GetAll_NoQueryString_UsesTheApiDefaultPageSizeOfSix()
  {
    var page = await GetPage("/api/v1/products");

    Assert.Equal(1, page.Page);
    Assert.Equal(6, page.PageSize);
    Assert.Equal(6, page.Items.Count);
  }

  [Fact]
  public async Task GetAll_QueryString_BindsPageSizeCategoryAndSearch()
  {
    var page = await GetPage("/api/v1/products?pageSize=3&category=tecnologia&search=teclado");

    Assert.Equal(3, page.PageSize);
    Assert.NotEmpty(page.Items);
    Assert.All(page.Items, item => Assert.Equal("tecnologia", item.CategorySlug));
    Assert.All(page.Items, item => Assert.Contains("eclado", item.Name, StringComparison.Ordinal));
  }

  [Fact]
  public async Task GetAll_UnknownCategory_ReturnsAnEmptyPageNotAnError()
  {
    var page = await GetPage("/api/v1/products?category=no-existe");

    Assert.Empty(page.Items);
    Assert.Equal(0, page.TotalItems);
    Assert.Equal(0, page.TotalPages);
  }

  // El tope existe para que nadie se traiga el catalogo entero en una peticion.
  [Fact]
  public async Task GetAll_PageSizeAboveTheCap_IsClampedToFifty()
  {
    var page = await GetPage("/api/v1/products?pageSize=500");

    Assert.Equal(50, page.PageSize);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-3)]
  public async Task GetAll_PageBelowOne_FallsBackToTheFirstPage(int page)
  {
    var result = await GetPage($"/api/v1/products?page={page}");

    Assert.Equal(1, result.Page);
    Assert.NotEmpty(result.Items);
  }

  // `page` y `pageSize` son `int`: un valor que no lo es lo rechaza el model binding, no el
  // servicio. Se fija el 400 para que nadie lo confunda con un catalogo vacio.
  [Fact]
  public async Task GetAll_NonNumericPage_ReturnsBadRequest()
  {
    var response = await _client.GetAsync(new Uri("/api/v1/products?page=abc", UriKind.Relative), TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task GetAll_PageBeyondTheLast_ReturnsNoItemsButKeepsTheTotal()
  {
    var page = await GetPage("/api/v1/products?page=99");

    Assert.Empty(page.Items);
    Assert.True(page.TotalItems > 0);
  }

  // ---------- GET /products/{publicId} ----------

  /// <summary>
  /// Los nombres del JSON son el contrato: `catalog.models.ts` los espeja uno a uno y ningun
  /// compilador une las dos orillas. Renombrar una propiedad del DTO rompe la pantalla en
  /// silencio, y esta prueba es lo unico que lo dice.
  /// </summary>
  [Fact]
  public async Task Get_ExistingSku_AnswersWithTheCamelCaseContractTheFrontendMirrors()
  {
    using var document = await GetJson("/api/v1/products/PRD-001");
    var root = document.RootElement;

    Assert.Equal("PRD-001", root.GetProperty("publicId").GetString());
    Assert.True(root.TryGetProperty("name", out _));
    Assert.True(root.TryGetProperty("description", out _));
    Assert.True(root.TryGetProperty("price", out _));
    Assert.True(root.TryGetProperty("imageUrl", out _));
    Assert.True(root.TryGetProperty("stock", out _));
    Assert.True(root.TryGetProperty("isActive", out _));

    var category = root.GetProperty("category");
    Assert.Equal("papeleria", category.GetProperty("slug").GetString());
    Assert.True(category.TryGetProperty("publicId", out _));
    Assert.True(category.TryGetProperty("name", out _));
  }

  // El SKU va en la ruta; el Id interno (Guid) no sale nunca. Es una decision de seguridad.
  [Fact]
  public async Task Get_ExistingSku_DoesNotLeakTheInternalId()
  {
    using var document = await GetJson("/api/v1/products/PRD-001");

    Assert.False(document.RootElement.TryGetProperty("id", out _));
    Assert.False(document.RootElement.GetProperty("category").TryGetProperty("id", out _));
  }

  [Fact]
  public async Task Get_UnknownSku_ReturnsNotFound()
  {
    var response = await _client.GetAsync(new Uri("/api/v1/products/PRD-999", UriKind.Relative), TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  // ---------- POST /products ----------

  [Fact]
  public async Task Create_ValidRequest_PersistsAndTheProductIsReadableBack()
  {
    var request = new CreateProductRequest(
      "PRD-901",
      "Grapadora industrial",
      "Grapadora de escritorio para 50 hojas.",
      74900m,
      "https://img/PRD-901.jpg",
      12,
      "papeleria");

    var response = await _client.PostAsJsonAsync(new Uri("/api/v1/products", UriKind.Relative), request, TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var stored = await _client.GetFromJsonAsync<ProductResponse>(
      new Uri("/api/v1/products/PRD-901", UriKind.Relative), TestContext.Current.CancellationToken);

    Assert.NotNull(stored);
    Assert.Equal("Grapadora industrial", stored.Name);
    Assert.Equal(74900m, stored.Price);
    // v1 sigue mandando una sola imageUrl; la API la guarda como la foto principal de la galeria.
    Assert.Equal("https://img/PRD-901.jpg", stored.ImageUrl);
    Assert.Equal("papeleria", stored.Category?.Slug);
  }

  // El fallo esperado sale como 400 con el texto del `Result`, no como una excepcion sin filtrar.
  [Fact]
  public async Task Create_UnknownCategory_ReturnsBadRequestWithTheDomainError()
  {
    var request = new CreateProductRequest(
      "PRD-902", "Producto suelto", "Sin categoria.", 1000m, "https://img/x.jpg", 1, "no-existe");

    var response = await _client.PostAsJsonAsync(new Uri("/api/v1/products", UriKind.Relative), request, TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("Category not found: no-existe", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
  }

  /// <summary>
  /// v1 no revisa el PublicId antes de guardar: el indice unico revienta en el repositorio, que
  /// lo convierte en `Result.Failure` y sale como 400. La prueba fija el estado y deja anotado
  /// que el cuerpo arrastra el texto del driver de SQLite — v2 si lo revisa antes y responde
  /// limpio, como muestra la prueba siguiente.
  /// </summary>
  [Fact]
  public async Task Create_PublicIdAlreadyTaken_ReturnsBadRequestCarryingTheDriverMessage()
  {
    var request = new CreateProductRequest(
      "PRD-001", "Resma repetida", "Duplicada.", 1000m, "https://img/x.jpg", 1, "papeleria");

    var response = await _client.PostAsJsonAsync(new Uri("/api/v1/products", UriKind.Relative), request, TestContext.Current.CancellationToken);
    var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.StartsWith("Failed to create product:", body, StringComparison.Ordinal);
  }

  [Fact]
  public async Task CreateV2_PublicIdAlreadyTaken_ReturnsBadRequestWithoutTheDatabaseMessage()
  {
    var request = new CreateProductV2Request(
      "PRD-001",
      "Resma repetida",
      "Duplicada.",
      1000m,
      1,
      "papeleria",
      [new CreateProductImageRequest("https://img/x.jpg", 0, true)],
      []);

    var response = await _client.PostAsJsonAsync(new Uri("/api/v2/products", UriKind.Relative), request, TestContext.Current.CancellationToken);
    var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("Public id already in use: PRD-001", body);
  }

  [Fact]
  public async Task CreateV2_ValidRequest_PersistsTheWholeGalleryAndItsVariants()
  {
    var request = new CreateProductV2Request(
      "PRD-903",
      "Silla apilable",
      "Silla apilable de visitante.",
      189000m,
      0,
      "mobiliario",
      [
        new CreateProductImageRequest("https://img/PRD-903-a.jpg", 0, false),
        new CreateProductImageRequest("https://img/PRD-903-b.jpg", 1, true)
      ],
      [new CreateProductVariantRequest("PRD-903-NEG", "Negro", 189000m, 4)]);

    var response = await _client.PostAsJsonAsync(new Uri("/api/v2/products", UriKind.Relative), request, TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    using var document = await GetJson("/api/v2/products/PRD-903");
    var root = document.RootElement;

    Assert.Equal(2, root.GetProperty("images").GetArrayLength());
    Assert.Equal(1, root.GetProperty("variants").GetArrayLength());
    // El stock del producto es la suma de sus presentaciones, no el 0 que trae el request.
    Assert.Equal(4, root.GetProperty("stock").GetInt32());
  }

  // ---------- Versionado ----------

  [Fact]
  public async Task Get_V2Route_ReturnsTheGalleryAndTheVariantsV1DoesNotHave()
  {
    using var document = await GetJson("/api/v2/products/PRD-001");
    var root = document.RootElement;

    Assert.Equal("PRD-001", root.GetProperty("publicId").GetString());
    Assert.Equal(JsonValueKind.Array, root.GetProperty("images").ValueKind);
    Assert.Equal(JsonValueKind.Array, root.GetProperty("variants").ValueKind);
    // v1 aplana la galeria en una sola `imageUrl`; v2 no la tiene.
    Assert.False(root.TryGetProperty("imageUrl", out _));
  }

  [Fact]
  public async Task GetAll_UnsupportedApiVersion_IsRejected()
  {
    var response = await _client.GetAsync(new Uri("/api/v9/products", UriKind.Relative), TestContext.Current.CancellationToken);

    Assert.False(response.IsSuccessStatusCode);
  }

  // ---------- Helpers ----------

  private async Task<PagedResult<ProductSummaryResponse>> GetPage(string url)
  {
    var page = await _client.GetFromJsonAsync<PagedResult<ProductSummaryResponse>>(
      new Uri(url, UriKind.Relative), TestContext.Current.CancellationToken);

    Assert.NotNull(page);

    return page;
  }

  private async Task<JsonDocument> GetJson(string url)
  {
    var response = await _client.GetAsync(new Uri(url, UriKind.Relative), TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
  }
}
