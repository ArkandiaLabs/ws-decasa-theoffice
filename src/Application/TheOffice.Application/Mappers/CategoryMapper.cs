using TheOffice.Domain.Entities;
using TheOffice.Application.DTOs;

namespace TheOffice.Application.Mappers;

public static class CategoryMapper
{
  public static CategoryResponse ToResponse(Category category)
  {
    return new CategoryResponse(category.PublicId, category.Name, category.Slug, category.Description);
  }
}
