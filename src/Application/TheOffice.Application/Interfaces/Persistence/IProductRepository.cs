using TheOffice.Application.DTOs;
using TheOffice.Domain.Common;
using TheOffice.Domain.Entities;

namespace TheOffice.Application.Interfaces.Persistence;

public interface IProductRepository
{
  Task<Result> Create(Product product);
  Task<Product?> GetByPublicId(string publicId);
  Task<(IReadOnlyList<ProductListItem> Items, int TotalItems)> GetPagedList(int page, int pageSize, string? categorySlug, string? search);
  Task<IReadOnlyList<string>> FindExistingPublicIds(string productPublicId, IReadOnlyList<string> variantPublicIds);
}
