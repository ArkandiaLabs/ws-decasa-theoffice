using TheOffice.Application.DTOs;
using TheOffice.Domain.Entities;

namespace TheOffice.Application.Mappers;

public static class ProductMapper
{
  public static Product ToDomain(CreateProductRequest request, Category category)
  {
    var product = new Product
    {
      PublicId = request.PublicId,
      Name = request.Name,
      Description = request.Description,
      Price = request.Price,
      ImageUrl = request.ImageUrl,
      Stock = request.Stock,
      IsActive = true,
      CategoryId = category.Id,
      Category = category
    };

    return product;
  }

  public static ProductSummaryResponse ToSummary(Product product)
  {
    return new ProductSummaryResponse(
      product.PublicId,
      product.Name,
      product.Price,
      product.ImageUrl,
      product.Stock,
      product.Category?.Name ?? string.Empty,
      product.Category?.Slug ?? string.Empty);
  }

  public static ProductResponse ToResponse(Product product)
  {
    return new ProductResponse(
      product.PublicId,
      product.Name,
      product.Description,
      product.Price,
      product.ImageUrl,
      product.Stock,
      product.IsActive,
      product.Category == null ? null : CategoryMapper.ToResponse(product.Category));
  }
}
