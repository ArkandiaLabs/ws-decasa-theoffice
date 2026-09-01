using System.Net;
using System.Net.Http.Json;

using TheOffice.Application.DTOs;

namespace TheOffice.Api.Tests.Controllers;

public class CategoryControllerTests : IClassFixture<ApiFactory>
{
  private readonly HttpClient _client;

  public CategoryControllerTests(ApiFactory factory)
  {
    _client = factory.CreateClient();
  }

  // Un arreglo plano, no una pagina: la fila de chips del listado lo consume tal cual.
  [Fact]
  public async Task GetAll_Always_ReturnsAFlatArrayOfCategories()
  {
    var categories = await _client.GetFromJsonAsync<List<CategoryResponse>>(
      new Uri("/api/v1/categories", UriKind.Relative), TestContext.Current.CancellationToken);

    Assert.NotNull(categories);
    Assert.NotEmpty(categories);
    Assert.All(categories, category => Assert.False(string.IsNullOrWhiteSpace(category.Slug)));
  }

  // El slug es lo que viaja en `?category=`; el nombre es lo que se pinta. No son lo mismo.
  [Fact]
  public async Task GetAll_Always_ExposesTheSlugTheListingFiltersBy()
  {
    var categories = await _client.GetFromJsonAsync<List<CategoryResponse>>(
      new Uri("/api/v1/categories", UriKind.Relative), TestContext.Current.CancellationToken);

    Assert.Contains(categories!, category => category.Slug == "papeleria" && category.Name == "Papeleria");
  }

  // Solo lectura: el catalogo no expone alta de categorias.
  [Fact]
  public async Task Post_Always_IsNotAllowed()
  {
    var response = await _client.PostAsync(new Uri("/api/v1/categories", UriKind.Relative), content: null, TestContext.Current.CancellationToken);

    Assert.False(response.IsSuccessStatusCode);
    Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
  }
}
