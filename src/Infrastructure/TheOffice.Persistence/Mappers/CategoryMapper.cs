using DomainEntities = TheOffice.Domain.Entities;
using PersistenceModels = TheOffice.Persistence.Models;

namespace TheOffice.Persistence.Mappers;

public static class CategoryMapper
{
  public static DomainEntities.Category ToDomain(PersistenceModels.Category category)
  {
    var domainCategory = new DomainEntities.Category();
    domainCategory.Id = category.Id;
    domainCategory.PublicId = category.PublicId;
    domainCategory.Name = category.Name;
    domainCategory.Slug = category.Slug;
    domainCategory.Description = category.Description;

    return domainCategory;
  }

  public static PersistenceModels.Category ToModel(DomainEntities.Category category)
  {
    var modelCategory = new PersistenceModels.Category();
    modelCategory.Id = category.Id;
    modelCategory.PublicId = category.PublicId;
    modelCategory.Name = category.Name;
    modelCategory.Slug = category.Slug;
    modelCategory.Description = category.Description;

    return modelCategory;
  }
}
