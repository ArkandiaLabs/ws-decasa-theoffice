using NSubstitute;

using TheOffice.Application.DTOs;
using TheOffice.Application.Interfaces.Adapters;
using TheOffice.Application.Interfaces.Persistence;
using TheOffice.Application.Services;
using TheOffice.Domain.Common;
using TheOffice.Domain.Entities;
using TheOffice.Domain.Enums;

namespace TheOffice.Application.Tests.Services;

public class CustomerServiceTests
{
  private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
  private readonly INotificationAdapter _notificationAdapter = Substitute.For<INotificationAdapter>();
  private readonly CustomerService _sut;

  public CustomerServiceTests()
  {
    _sut = new CustomerService(_customerRepository, _notificationAdapter);
  }

  // ---------- Create ----------

  [Fact]
  public async Task Create_InvalidSource_ReturnsFailureNamingTheValue()
  {
    var result = await _sut.Create(BuildRequest(source: "Fax"));

    Assert.False(result.IsSuccess);
    Assert.Equal("Invalid customer source: Fax", result.Error);
  }

  // Un origen que no existe se rechaza antes de tocar nada: ni escribe ni avisa.
  [Fact]
  public async Task Create_InvalidSource_NeitherPersistsNorNotifies()
  {
    await _sut.Create(BuildRequest(source: "Fax"));

    await _customerRepository.DidNotReceiveWithAnyArgs().Create(default!);
    await _notificationAdapter.DidNotReceiveWithAnyArgs().SendMessage(default!);
  }

  [Theory]
  [InlineData("Website", CustomerSource.Website)]
  [InlineData("website", CustomerSource.Website)]
  [InlineData("SOCIALMEDIA", CustomerSource.SocialMedia)]
  [InlineData("phone", CustomerSource.Phone)]
  public async Task Create_SourceInAnyCase_IsParsedIgnoringCase(string source, CustomerSource expected)
  {
    StubSuccessfulCreate();

    var result = await _sut.Create(BuildRequest(source: source));

    Assert.True(result.IsSuccess);
    await _customerRepository.Received(1).Create(Arg.Is<Customer>(x => x!.Source == expected));
  }

  // El enum se acepta tambien por su numero: `Enum.TryParse` lo hace y nadie lo bloquea.
  // La prueba lo fija como comportamiento conocido, no como algo que el cliente deba usar.
  [Fact]
  public async Task Create_NumericSource_IsAcceptedByEnumTryParse()
  {
    StubSuccessfulCreate();

    var result = await _sut.Create(BuildRequest(source: "3"));

    Assert.True(result.IsSuccess);
    Assert.Equal("SocialMedia", result.Value!.Source);
  }

  [Fact]
  public async Task Create_ValidRequest_MapsEveryFieldOntoTheDomainCustomer()
  {
    StubSuccessfulCreate();

    await _sut.Create(new CreateCustomerRequest("CUS-010", "Papeleria Central", "compras@central.co", "Email"));

    await _customerRepository.Received(1).Create(Arg.Is<Customer>(x =>
      x!.PublicId == "CUS-010"
      && x.Name == "Papeleria Central"
      && x.Email == "compras@central.co"
      && x.Source == CustomerSource.Email));
  }

  [Fact]
  public async Task Create_ValidRequest_ReturnsTheCustomerWithTheSourceAsItsName()
  {
    StubSuccessfulCreate();

    var result = await _sut.Create(BuildRequest(source: "SocialMedia"));

    Assert.True(result.IsSuccess);
    Assert.Equal(
      new CustomerResponse("CUS-001", "Oficinas Andina", "compras@andina.co", "SocialMedia"),
      result.Value);
  }

  [Fact]
  public async Task Create_ValidRequest_NotifiesWithThePublicId()
  {
    StubSuccessfulCreate();

    await _sut.Create(BuildRequest());

    await _notificationAdapter.Received(1).SendMessage("New customer created: CUS-001");
  }

  [Fact]
  public async Task Create_RepositoryFails_ReturnsThatSameError()
  {
    _customerRepository.Create(Arg.Any<Customer>()).Returns(Result.Failure("Failed to create customer: UNIQUE constraint failed"));

    var result = await _sut.Create(BuildRequest());

    Assert.False(result.IsSuccess);
    Assert.Equal("Failed to create customer: UNIQUE constraint failed", result.Error);
  }

  // Avisar de un cliente que no se guardo es peor que no avisar: el aviso va despues del exito.
  [Fact]
  public async Task Create_RepositoryFails_DoesNotNotify()
  {
    _customerRepository.Create(Arg.Any<Customer>()).Returns(Result.Failure("boom"));

    await _sut.Create(BuildRequest());

    await _notificationAdapter.DidNotReceiveWithAnyArgs().SendMessage(default!);
  }

  // ---------- GetByPublicId ----------

  [Fact]
  public async Task GetByPublicId_ExistingCustomer_ReturnsSuccessResult()
  {
    _customerRepository.GetByPublicId("CUS-001").Returns(BuildCustomer());

    var result = await _sut.GetByPublicId("CUS-001");

    Assert.NotNull(result);
    Assert.True(result.IsSuccess);
    Assert.Equal(
      new CustomerResponse("CUS-001", "Oficinas Andina", "compras@andina.co", "Website"),
      result.Value);
  }

  // Ausente es `null`, no un `Result.Failure`: el controller lo traduce a 404 y no a 400.
  [Fact]
  public async Task GetByPublicId_MissingCustomer_ReturnsNull()
  {
    _customerRepository.GetByPublicId("CUS-999").Returns((Customer?)null);

    var result = await _sut.GetByPublicId("CUS-999");

    Assert.Null(result);
  }

  [Fact]
  public async Task GetByPublicId_Always_PassesThePublicIdToTheRepository()
  {
    _customerRepository.GetByPublicId(Arg.Any<string>()).Returns((Customer?)null);

    await _sut.GetByPublicId("CUS-042");

    await _customerRepository.Received(1).GetByPublicId("CUS-042");
  }

  // ---------- Helpers ----------

  private void StubSuccessfulCreate()
  {
    _customerRepository.Create(Arg.Any<Customer>()).Returns(Result.Success());
  }

  private static CreateCustomerRequest BuildRequest(string source = "Website")
  {
    return new CreateCustomerRequest("CUS-001", "Oficinas Andina", "compras@andina.co", source);
  }

  private static Customer BuildCustomer()
  {
    return new Customer
    {
      Id = Guid.NewGuid(),
      PublicId = "CUS-001",
      Name = "Oficinas Andina",
      Email = "compras@andina.co",
      Source = CustomerSource.Website
    };
  }
}
