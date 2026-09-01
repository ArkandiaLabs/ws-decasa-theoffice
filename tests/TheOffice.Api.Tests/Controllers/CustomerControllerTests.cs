using System.Net;
using System.Net.Http.Json;

using TheOffice.Application.DTOs;

namespace TheOffice.Api.Tests.Controllers;

public class CustomerControllerTests : IClassFixture<ApiFactory>
{
  private readonly HttpClient _client;

  public CustomerControllerTests(ApiFactory factory)
  {
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task Create_ValidRequest_ReturnsTheCustomerWithItsSourceAsText()
  {
    var request = new CreateCustomerRequest("CUS-901", "Oficinas Andina", "compras@andina.co", "Email");

    var response = await _client.PostAsJsonAsync(new Uri("/api/v1/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal(
      new CustomerResponse("CUS-901", "Oficinas Andina", "compras@andina.co", "Email"),
      await response.Content.ReadFromJsonAsync<CustomerResponse>(TestContext.Current.CancellationToken));
  }

  // El origen es un enum del dominio y llega como texto: un valor fuera del enum es 400.
  [Fact]
  public async Task Create_InvalidSource_ReturnsBadRequestNamingTheValue()
  {
    var request = new CreateCustomerRequest("CUS-902", "Oficinas Andina", "compras@andina.co", "Fax");

    var response = await _client.PostAsJsonAsync(new Uri("/api/v1/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("Invalid customer source: Fax", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
  }

  [Fact]
  public async Task Create_PublicIdAlreadyTaken_ReturnsBadRequest()
  {
    var request = new CreateCustomerRequest("CUS-001", "Cliente repetido", "repetido@example.com", "Website");

    var response = await _client.PostAsJsonAsync(new Uri("/api/v1/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Get_ExistingPublicId_ReturnsTheSeededCustomer()
  {
    var customer = await _client.GetFromJsonAsync<CustomerResponse>(
      new Uri("/api/v1/customers/CUS-001", UriKind.Relative), TestContext.Current.CancellationToken);

    Assert.NotNull(customer);
    Assert.Equal("CUS-001", customer.PublicId);
    Assert.Equal("Website", customer.Source);
  }

  [Fact]
  public async Task Get_UnknownPublicId_ReturnsNotFound()
  {
    var response = await _client.GetAsync(new Uri("/api/v1/customers/CUS-999", UriKind.Relative), TestContext.Current.CancellationToken);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }
}
