using DomainEntities = TheOffice.Domain.Entities;
using PersistenceModels = TheOffice.Persistence.Models;

namespace TheOffice.Persistence.Mappers;

public static class ProductImageMapper
{
  public static DomainEntities.ProductImage ToDomain(PersistenceModels.ProductImage image)
  {
    var domainImage = new DomainEntities.ProductImage();
    domainImage.Id = image.Id;
    domainImage.PublicId = image.PublicId;
    domainImage.Url = image.Url;
    domainImage.SortOrder = image.SortOrder;
    domainImage.IsPrimary = image.IsPrimary;
    domainImage.ProductId = image.ProductId;

    return domainImage;
  }

  public static PersistenceModels.ProductImage ToModel(DomainEntities.ProductImage image)
  {
    var modelImage = new PersistenceModels.ProductImage();
    modelImage.Id = image.Id;
    modelImage.PublicId = image.PublicId;
    modelImage.Url = image.Url;
    modelImage.SortOrder = image.SortOrder;
    modelImage.IsPrimary = image.IsPrimary;
    modelImage.ProductId = image.ProductId;

    return modelImage;
  }
}
