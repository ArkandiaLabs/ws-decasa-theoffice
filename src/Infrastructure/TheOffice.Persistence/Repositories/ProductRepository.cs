using Microsoft.EntityFrameworkCore;

using TheOffice.Application.DTOs;
using TheOffice.Application.Interfaces.Persistence;
using TheOffice.Domain.Common;
using TheOffice.Persistence.Mappers;

using DomainEntities = TheOffice.Domain.Entities;

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
      .Include(x => x.Images)
      .Include(x => x.Variants)
      .SingleOrDefaultAsync(x => x.PublicId == publicId);

    return product != null ? ProductMapper.ToDomain(product) : null;
  }

  public async Task<(IReadOnlyList<ProductListItem> Items, int TotalItems)> GetPagedList(
    int page, int pageSize, string? categorySlug, string? search)
  {
    var query = _context.Products
      .AsNoTracking()
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

    // El orden de la foto principal es el mismo que aplica ProductMapper sobre el detalle:
    // la marcada, si no la de menor SortOrder, y PublicId para desempatar. Si las dos
    // expresiones divergen, el listado y el detalle devuelven imageUrl distintas.
    var items = await query
      .OrderBy(x => x.Name)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(x => new ProductListItem(
        x.PublicId,
        x.Name,
        x.Price,
        x.Variants.Count == 0 ? x.Stock : x.Variants.Sum(v => v.Stock),
        x.Category.Name,
        x.Category.Slug,
        x.Images
          .OrderByDescending(i => i.IsPrimary)
          .ThenBy(i => i.SortOrder)
          .ThenBy(i => i.PublicId)
          .Select(i => new ProductImageResponse(i.PublicId, i.Url, i.SortOrder, i.IsPrimary))
          .FirstOrDefault(),
        x.Variants.Count))
      .ToListAsync();

    return (items, totalItems);
  }

  // Los PublicId ya tomados, para que el servicio devuelva un Result.Failure propio en vez
  // de dejar que reviente el indice unico y se filtre el mensaje de SQLite al cliente.
  public async Task<IReadOnlyList<string>> FindExistingPublicIds(string productPublicId, IReadOnlyList<string> variantPublicIds)
  {
    var taken = new List<string>();

    if (await _context.Products.AsNoTracking().AnyAsync(x => x.PublicId == productPublicId))
    {
      taken.Add(productPublicId);
    }

    if (variantPublicIds.Count > 0)
    {
      var existing = await _context.ProductVariants
        .AsNoTracking()
        .Where(x => variantPublicIds.Contains(x.PublicId))
        .Select(x => x.PublicId)
        .ToListAsync();

      taken.AddRange(existing);
    }

    return taken;
  }
}
