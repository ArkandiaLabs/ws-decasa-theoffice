using TheOffice.Domain.Common;
using TheOffice.Domain.Entities;

namespace TheOffice.Application.Interfaces.Persistence;

public interface IProductRepository
{
  Task<Result> Create(Product product);
  Task<Product?> GetByPublicId(string publicId);
  Task<(IReadOnlyList<Product> Items, int TotalItems)> GetPaged(int page, int pageSize, string? categorySlug, string? search);
}
