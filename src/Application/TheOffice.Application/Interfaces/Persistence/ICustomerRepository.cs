using TheOffice.Domain.Common;
using TheOffice.Domain.Entities;

namespace TheOffice.Application.Interfaces.Persistence;

public interface ICustomerRepository
{
  Task<Result> Create(Customer customer);
  Task<Customer?> GetByPublicId(string publicId);
}
