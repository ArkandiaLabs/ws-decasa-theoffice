using TheOffice.Application.DTOs;
using TheOffice.Domain.Entities;

namespace TheOffice.Application.Mappers;

public static class ProductMapper
{
  public static Product ToDomain(CreateProductRequest request, Category category)
  {
    // El Id se asigna aqui, y no al guardar, porque la imagen que v1 crea necesita apuntar
    // al producto antes de que EF Core lo persista.
    var productId = Guid.NewGuid();

    var product = new Product
    {
      Id = productId,
      PublicId = request.PublicId,
      Name = request.Name,
      Description = request.Description,
      Price = request.Price,
      Stock = request.Stock,
      IsActive = true,
      CategoryId = category.Id,
      Category = category
    };

    // v1 sigue mandando una sola imageUrl. Se vuelve la unica foto de la galeria, marcada
    // como principal, para que la respuesta de v1 no cambie ahora que ImageUrl no existe.
    product.Images.Add(new ProductImage
    {
      Id = Guid.NewGuid(),
      PublicId = $"{request.PublicId}-IMG-1",
      Url = request.ImageUrl,
      SortOrder = 0,
      IsPrimary = true,
      ProductId = productId
    });

    return product;
  }

  // La foto principal: la marcada; si ninguna lo esta, la de menor SortOrder. El desempate
  // por PublicId mantiene la respuesta estable cuando dos fotos comparten SortOrder.
  public static string ResolvePrimaryImageUrl(Product product)
  {
    return product.Images
      .OrderByDescending(x => x.IsPrimary)
      .ThenBy(x => x.SortOrder)
      .ThenBy(x => x.PublicId, StringComparer.Ordinal)
      .FirstOrDefault()?.Url ?? string.Empty;
  }

  // Las existencias del producto son la suma de sus presentaciones; su propio Stock cuando
  // no tiene ninguna. Es la derivacion que mantiene congelado el contrato de v1.
  public static int ResolveStock(Product product)
  {
    return product.Variants.Count == 0
      ? product.Stock
      : product.Variants.Sum(x => x.Stock);
  }

  public static ProductSummaryResponse ToSummary(Product product)
  {
    return new ProductSummaryResponse(
      product.PublicId,
      product.Name,
      product.Price,
      ResolvePrimaryImageUrl(product),
      ResolveStock(product),
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
      ResolvePrimaryImageUrl(product),
      ResolveStock(product),
      product.IsActive,
      product.Category == null ? null : CategoryMapper.ToResponse(product.Category));
  }
}
