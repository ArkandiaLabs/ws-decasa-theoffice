using TheOffice.Application.DTOs;
using TheOffice.Domain.Entities;
using TheOffice.Domain.Enums;

namespace TheOffice.Application.Mappers;

public static class CustomerMapper
{
  public static Customer ToDomain(CreateCustomerRequest request, CustomerSource source)
  {
    var customer = new Customer
    {
      PublicId = request.PublicId,
      Name = request.Name,
      Email = request.Email,
      Source = source
    };

    return customer;
  }

  public static CustomerResponse ToResponse(Customer customer)
  {
    return new CustomerResponse(customer.PublicId, customer.Name, customer.Email, customer.Source.ToString());
  }
}
