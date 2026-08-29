using DomainEntities = TheOffice.Domain.Entities;
using PersistenceModels = TheOffice.Persistence.Models;

namespace TheOffice.Persistence.Mappers;

public static class ProductVariantMapper
{
  public static DomainEntities.ProductVariant ToDomain(PersistenceModels.ProductVariant variant)
  {
    var domainVariant = new DomainEntities.ProductVariant();
    domainVariant.Id = variant.Id;
    domainVariant.PublicId = variant.PublicId;
    domainVariant.Name = variant.Name;
    domainVariant.Price = variant.Price;
    domainVariant.Stock = variant.Stock;
    domainVariant.ProductId = variant.ProductId;

    return domainVariant;
  }

  public static PersistenceModels.ProductVariant ToModel(DomainEntities.ProductVariant variant)
  {
    var modelVariant = new PersistenceModels.ProductVariant();
    modelVariant.Id = variant.Id;
    modelVariant.PublicId = variant.PublicId;
    modelVariant.Name = variant.Name;
    modelVariant.Price = variant.Price;
    modelVariant.Stock = variant.Stock;
    modelVariant.ProductId = variant.ProductId;

    return modelVariant;
  }
}
