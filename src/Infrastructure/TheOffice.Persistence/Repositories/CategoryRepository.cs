using Microsoft.EntityFrameworkCore;

using DomainEntities = TheOffice.Domain.Entities;
using TheOffice.Application.Interfaces.Persistence;
using TheOffice.Persistence.Mappers;

namespace TheOffice.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
  private readonly TheOfficeDbContext _context;

  public CategoryRepository(TheOfficeDbContext context)
  {
    _context = context;
  }

  public async Task<IReadOnlyList<DomainEntities.Category>> GetAll()
  {
    var categories = await _context.Categories
      .AsNoTracking()
      .OrderBy(x => x.Name)
      .ToListAsync();

    return categories.Select(CategoryMapper.ToDomain).ToList();
  }

  public async Task<DomainEntities.Category?> GetBySlug(string slug)
  {
    var category = await _context.Categories
      .AsNoTracking()
      .SingleOrDefaultAsync(x => x.Slug == slug);

    return category != null ? CategoryMapper.ToDomain(category) : null;
  }
}
