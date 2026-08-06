using TheOffice.Domain.Common;
using TheOffice.Domain.Enums;
using TheOffice.Application.DTOs;
using TheOffice.Application.Mappers;
using TheOffice.Application.Interfaces.Persistence;
using TheOffice.Application.Interfaces.Adapters;

namespace TheOffice.Application.Services;

public class CustomerService
{
  private readonly ICustomerRepository _customerRepository;
  private readonly INotificationAdapter _notificationAdapter;

  public CustomerService(ICustomerRepository customerRepository, INotificationAdapter notificationAdapter)
  {
    _customerRepository = customerRepository;
    _notificationAdapter = notificationAdapter;
  }

  public async Task<Result<CustomerResponse>> Create(CreateCustomerRequest request)
  {
    if (!Enum.TryParse<CustomerSource>(request.Source, ignoreCase: true, out var source))
    {
      return Result.Failure<CustomerResponse>($"Invalid customer source: {request.Source}");
    }

    var customer = CustomerMapper.ToDomain(request, source);
    var result = await _customerRepository.Create(customer);
    if (!result.IsSuccess)
    {
      return Result.Failure<CustomerResponse>(result.Error!);
    }

    await _notificationAdapter.SendMessage($"New customer created: {customer.PublicId}");

    return Result.Success(CustomerMapper.ToResponse(customer));
  }

  public async Task<Result<CustomerResponse>?> GetByPublicId(string publicId)
  {
    var customer = await _customerRepository.GetByPublicId(publicId);

    return customer == null ? null : Result.Success(CustomerMapper.ToResponse(customer));
  }
}
