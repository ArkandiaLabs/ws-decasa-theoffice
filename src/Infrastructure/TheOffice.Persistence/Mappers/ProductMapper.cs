using DomainEntities = TheOffice.Domain.Entities;
using PersistenceModels = TheOffice.Persistence.Models;

namespace TheOffice.Persistence.Mappers;

public static class ProductMapper
{
  public static DomainEntities.Product ToDomain(PersistenceModels.Product product)
  {
    var domainProduct = new DomainEntities.Product();
    domainProduct.Id = product.Id;
    domainProduct.PublicId = product.PublicId;
    domainProduct.Name = product.Name;
    domainProduct.Description = product.Description;
    domainProduct.Price = product.Price;
    domainProduct.ImageUrl = product.ImageUrl;
    domainProduct.Stock = product.Stock;
    domainProduct.IsActive = product.IsActive;
    domainProduct.CategoryId = product.CategoryId;
    domainProduct.Category = product.Category == null ? null : CategoryMapper.ToDomain(product.Category);

    return domainProduct;
  }

  public static PersistenceModels.Product ToModel(DomainEntities.Product product)
  {
    var modelProduct = new PersistenceModels.Product();
    modelProduct.Id = product.Id;
    modelProduct.PublicId = product.PublicId;
    modelProduct.Name = product.Name;
    modelProduct.Description = product.Description;
    modelProduct.Price = product.Price;
    modelProduct.ImageUrl = product.ImageUrl;
    modelProduct.Stock = product.Stock;
    modelProduct.IsActive = product.IsActive;
    modelProduct.CategoryId = product.CategoryId;

    return modelProduct;
  }
}
