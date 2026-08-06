using Microsoft.EntityFrameworkCore;

using TheOffice.Domain.Common;
using DomainEntities = TheOffice.Domain.Entities;
using TheOffice.Application.Interfaces.Persistence;
using TheOffice.Persistence.Mappers;

namespace TheOffice.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
  private readonly TheOfficeDbContext _context;

  public ProductRepository(TheOfficeDbContext context)
  {
    _context = context;
  }

  public async Task<Result> Create(DomainEntities.Product product)
  {
    try
    {
      var modelProduct = ProductMapper.ToModel(product);

      await _context.Products.AddAsync(modelProduct);
      await _context.SaveChangesAsync();

      product.Id = modelProduct.Id;

      return Result.Success();
    }
    catch (Exception ex)
    {
      return Result.Failure($"Failed to create product: {ex.Message}");
    }
  }

  public async Task<DomainEntities.Product?> GetByPublicId(string publicId)
  {
    var product = await _context.Products
      .AsNoTracking()
      .Include(x => x.Category)
      .SingleOrDefaultAsync(x => x.PublicId == publicId);

    return product != null ? ProductMapper.ToDomain(product) : null;
  }

  public async Task<(IReadOnlyList<DomainEntities.Product> Items, int TotalItems)> GetPaged(
    int page, int pageSize, string? categorySlug, string? search)
  {
    var query = _context.Products
      .AsNoTracking()
      .Include(x => x.Category)
      .Where(x => x.IsActive);

    if (!string.IsNullOrWhiteSpace(categorySlug))
    {
      var slug = categorySlug.Trim();
      query = query.Where(x => x.Category.Slug == slug);
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
      var term = $"%{search.Trim()}%";
      query = query.Where(x => EF.Functions.Like(x.Name, term) || EF.Functions.Like(x.Description, term));
    }

    var totalItems = await query.CountAsync();

    var products = await query
      .OrderBy(x => x.Name)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync();

    return (products.Select(ProductMapper.ToDomain).ToList(), totalItems);
  }
}
