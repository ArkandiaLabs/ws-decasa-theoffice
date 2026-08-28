using TheOffice.Application.DTOs;
using TheOffice.Application.Interfaces.Adapters;
using TheOffice.Application.Interfaces.Persistence;
using TheOffice.Application.Mappers;
using TheOffice.Domain.Common;

namespace TheOffice.Application.Services;

public class ProductService
{
  private const int MaxPageSize = 50;
  private const int MaxUrlLength = 500;
  private const int MaxNameLength = 150;
  // El PublicId de cada foto se deriva del producto como {publicId}-IMG-{n} y la columna
  // admite 50, asi que el del producto se acota mas corto para que el derivado quepa.
  private const int MaxProductPublicIdLength = 40;
  private const int MaxVariantNameLength = 100;

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

  public async Task<PagedResult<ProductSummaryV2Response>> GetAllV2(ProductQuery query)
  {
    var page = query.Page < 1 ? 1 : query.Page;
    var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

    var (products, totalItems) = await _productRepository.GetPagedList(page, pageSize, query.Category, query.Search);

    return new PagedResult<ProductSummaryV2Response>(
      products.Select(ProductMapper.ToSummaryV2).ToList(),
      page,
      pageSize,
      totalItems);
  }

  public async Task<Result<ProductV2Response>?> GetByPublicIdV2(string publicId)
  {
    var product = await _productRepository.GetByPublicId(publicId);

    return product == null ? null : Result.Success(ProductMapper.ToResponseV2(product));
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

  public async Task<Result<ProductV2Response>> CreateV2(CreateProductV2Request request)
  {
    // Un request sin la clave y uno con la lista vacia son la misma falla: el binder de
    // JSON deja null cuando la propiedad no viene, aunque el record la declare no nullable.
    var images = request.Images ?? [];
    var variants = request.Variants ?? [];

    if (string.IsNullOrWhiteSpace(request.PublicId) || request.PublicId.Length > MaxProductPublicIdLength)
    {
      return Result.Failure<ProductV2Response>("Public id is required and cannot exceed 40 characters");
    }

    if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > MaxNameLength)
    {
      return Result.Failure<ProductV2Response>("Name is required and cannot exceed 150 characters");
    }

    if (images.Count == 0)
    {
      return Result.Failure<ProductV2Response>("A product needs at least one image");
    }

    if (images.Count(x => x.IsPrimary) > 1)
    {
      return Result.Failure<ProductV2Response>("Only one image can be marked as primary");
    }

    foreach (var image in images)
    {
      // SQLite no aplica VARCHAR(n) y EF Core no valida StringLength al guardar, asi que
      // las cotas se revisan aqui o no se revisan en ninguna parte.
      if (string.IsNullOrWhiteSpace(image.Url) || image.Url.Length > MaxUrlLength)
      {
        return Result.Failure<ProductV2Response>("Image url is required and cannot exceed 500 characters");
      }
    }

    if (request.Price < 0 || request.Stock < 0)
    {
      return Result.Failure<ProductV2Response>("Price and stock cannot be negative");
    }

    foreach (var variant in variants)
    {
      if (string.IsNullOrWhiteSpace(variant.PublicId))
      {
        return Result.Failure<ProductV2Response>("Every variant needs a public id");
      }

      if (string.IsNullOrWhiteSpace(variant.Name) || variant.Name.Length > MaxVariantNameLength)
      {
        return Result.Failure<ProductV2Response>($"Variant name is required and cannot exceed 100 characters: {variant.PublicId}");
      }

      if (variant.Price < 0 || variant.Stock < 0)
      {
        return Result.Failure<ProductV2Response>($"Variant price and stock cannot be negative: {variant.PublicId}");
      }
    }

    var duplicated = variants
      .GroupBy(x => x.PublicId, StringComparer.OrdinalIgnoreCase)
      .FirstOrDefault(x => x.Count() > 1);
    if (duplicated != null)
    {
      return Result.Failure<ProductV2Response>($"Duplicated variant public id: {duplicated.Key}");
    }

    var category = await _categoryRepository.GetBySlug(request.CategorySlug);
    if (category == null)
    {
      return Result.Failure<ProductV2Response>($"Category not found: {request.CategorySlug}");
    }

    var taken = await _productRepository.FindExistingPublicIds(
      request.PublicId,
      variants.Select(x => x.PublicId).ToList());
    if (taken.Count > 0)
    {
      return Result.Failure<ProductV2Response>($"Public id already in use: {string.Join(", ", taken)}");
    }

    var product = ProductMapper.ToDomain(request, category);
    var result = await _productRepository.Create(product);
    if (!result.IsSuccess)
    {
      return Result.Failure<ProductV2Response>(result.Error!);
    }

    await _notificationAdapter.SendMessage($"New product created: {product.PublicId}");

    return Result.Success(ProductMapper.ToResponseV2(product));
  }
}
