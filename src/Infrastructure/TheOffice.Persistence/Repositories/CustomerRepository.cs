using Microsoft.EntityFrameworkCore;

using TheOffice.Application.Interfaces.Persistence;
using TheOffice.Domain.Common;
using TheOffice.Persistence.Mappers;

using DomainEntities = TheOffice.Domain.Entities;

namespace TheOffice.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
  private readonly TheOfficeDbContext _context;

  public CustomerRepository(TheOfficeDbContext context)
  {
    _context = context;
  }

  public async Task<Result> Create(DomainEntities.Customer customer)
  {
    try
    {
      var modelCustomer = CustomerMapper.ToModel(customer);

      await _context.Customers.AddAsync(modelCustomer);
      await _context.SaveChangesAsync();

      customer.Id = modelCustomer.Id;

      return Result.Success();
    }
    catch (Exception ex)
    {
      return Result.Failure($"Failed to create customer: {ex.Message}");
    }
  }

  public async Task<DomainEntities.Customer?> GetByPublicId(string publicId)
  {
    var customer = await _context.Customers
      .AsNoTracking()
      .SingleOrDefaultAsync(x => x.PublicId == publicId);

    return customer != null ? CustomerMapper.ToDomain(customer) : null;
  }
}
