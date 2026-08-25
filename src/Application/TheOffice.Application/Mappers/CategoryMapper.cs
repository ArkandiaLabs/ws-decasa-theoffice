using TheOffice.Application.DTOs;
using TheOffice.Domain.Entities;

namespace TheOffice.Application.Mappers;

public static class CategoryMapper
{
  public static CategoryResponse ToResponse(Category category)
  {
    return new CategoryResponse(category.PublicId, category.Name, category.Slug, category.Description);
  }
}
