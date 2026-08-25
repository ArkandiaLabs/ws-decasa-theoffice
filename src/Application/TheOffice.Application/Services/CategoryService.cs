using TheOffice.Application.DTOs;
using TheOffice.Application.Interfaces.Persistence;
using TheOffice.Application.Mappers;

namespace TheOffice.Application.Services;

public class CategoryService
{
  private readonly ICategoryRepository _categoryRepository;

  public CategoryService(ICategoryRepository categoryRepository)
  {
    _categoryRepository = categoryRepository;
  }

  public async Task<IReadOnlyList<CategoryResponse>> GetAll()
  {
    var categories = await _categoryRepository.GetAll();

    return categories.Select(CategoryMapper.ToResponse).ToList();
  }
}
