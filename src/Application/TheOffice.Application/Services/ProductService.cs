using TheOffice.Application.DTOs;
using TheOffice.Application.Interfaces.Adapters;
using TheOffice.Application.Interfaces.Persistence;
using TheOffice.Application.Mappers;
using TheOffice.Domain.Common;

namespace TheOffice.Application.Services;

public class ProductService
{
  private const int MaxPageSize = 50;

  private readonly IProductRepository _productRepository;
  private readonly ICategoryRepository _categoryRepository;
  private readonly INotificationAdapter _notificationAdapter;

  public ProductService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    INotificationAdapter notificationAdapter)
  {
    _productRepository = productRepository;
    _categoryRepository = categoryRepository;
    _notificationAdapter = notificationAdapter;
  }

  public async Task<PagedResult<ProductSummaryResponse>> GetAll(ProductQuery query)
  {
    var page = query.Page < 1 ? 1 : query.Page;
    var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

    var (products, totalItems) = await _productRepository.GetPagedList(page, pageSize, query.Category, query.Search);

    return new PagedResult<ProductSummaryResponse>(
      products.Select(ProductMapper.ToSummary).ToList(),
      page,
      pageSize,
      totalItems);
  }

  public async Task<Result<ProductResponse>?> GetByPublicId(string publicId)
  {
    var product = await _productRepository.GetByPublicId(publicId);

    return product == null ? null : Result.Success(ProductMapper.ToResponse(product));
  }

  public async Task<Result<ProductResponse>> Create(CreateProductRequest request)
  {
    var category = await _categoryRepository.GetBySlug(request.CategorySlug);
    if (category == null)
    {
      return Result.Failure<ProductResponse>($"Category not found: {request.CategorySlug}");
    }

    var product = ProductMapper.ToDomain(request, category);
    var result = await _productRepository.Create(product);
    if (!result.IsSuccess)
    {
      return Result.Failure<ProductResponse>(result.Error!);
    }

    await _notificationAdapter.SendMessage($"New product created: {product.PublicId}");

    return Result.Success(ProductMapper.ToResponse(product));
  }
}
