using DomainEntities = TheOffice.Domain.Entities;
using PersistenceModels = TheOffice.Persistence.Models;

namespace TheOffice.Persistence.Mappers;

public static class CustomerMapper
{
  public static DomainEntities.Customer ToDomain(PersistenceModels.Customer customer)
  {
    var domainCustomer = new DomainEntities.Customer();
    domainCustomer.Id = customer.Id;
    domainCustomer.PublicId = customer.PublicId;
    domainCustomer.Name = customer.Name;
    domainCustomer.Email = customer.Email;
    domainCustomer.Source = customer.Source;

    return domainCustomer;
  }

  public static PersistenceModels.Customer ToModel(DomainEntities.Customer customer)
  {
    var modelCustomer = new PersistenceModels.Customer();
    modelCustomer.Id = customer.Id;
    modelCustomer.PublicId = customer.PublicId;
    modelCustomer.Name = customer.Name;
    modelCustomer.Email = customer.Email;
    modelCustomer.Source = customer.Source;

    return modelCustomer;
  }
}
