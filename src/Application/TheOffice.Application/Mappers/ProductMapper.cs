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

  public static Product ToDomain(CreateProductV2Request request, Category category)
  {
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

    var images = request.Images.OrderBy(x => x.SortOrder).ToList();

    // Si nadie marco la principal, gana la de menor SortOrder y queda marcada en la fila.
    // Derivarla solo al leer dejaria a la base sin registrar cual eligio quien carga.
    var primaryIndex = images.FindIndex(x => x.IsPrimary);
    if (primaryIndex < 0)
    {
      primaryIndex = 0;
    }

    for (var i = 0; i < images.Count; i++)
    {
      product.Images.Add(new ProductImage
      {
        Id = Guid.NewGuid(),
        PublicId = $"{request.PublicId}-IMG-{i + 1}",
        Url = images[i].Url,
        SortOrder = images[i].SortOrder,
        IsPrimary = i == primaryIndex,
        ProductId = productId
      });
    }

    foreach (var variant in request.Variants)
    {
      product.Variants.Add(new ProductVariant
      {
        Id = Guid.NewGuid(),
        PublicId = variant.PublicId,
        Name = variant.Name,
        Price = variant.Price,
        Stock = variant.Stock,
        ProductId = productId
      });
    }

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

  // El listado ya llega derivado desde el repositorio: la foto principal y el stock
  // agregado se resuelven en SQL con la misma regla que ResolvePrimaryImageUrl y
  // ResolveStock aplican sobre el detalle.
  public static ProductSummaryResponse ToSummary(ProductListItem item)
  {
    return new ProductSummaryResponse(
      item.PublicId,
      item.Name,
      item.Price,
      item.PrimaryImage?.Url ?? string.Empty,
      item.Stock,
      item.CategoryName,
      item.CategorySlug);
  }

  public static ProductSummaryV2Response ToSummaryV2(ProductListItem item)
  {
    return new ProductSummaryV2Response(
      item.PublicId,
      item.Name,
      item.Price,
      item.PrimaryImage,
      item.Stock,
      item.Stock > 0,
      item.VariantCount,
      item.CategoryName,
      item.CategorySlug);
  }

  public static ProductV2Response ToResponseV2(Product product)
  {
    return new ProductV2Response(
      product.PublicId,
      product.Name,
      product.Description,
      product.Price,
      ResolveStock(product),
      product.IsActive,
      // El orden de la galeria lo define quien carga, no la base: el Include no garantiza
      // ninguno, asi que se ordena aqui.
      product.Images
        .OrderBy(x => x.SortOrder)
        .ThenBy(x => x.PublicId, StringComparer.Ordinal)
        .Select(x => new ProductImageResponse(x.PublicId, x.Url, x.SortOrder, x.IsPrimary))
        .ToList(),
      // Ninguna presentacion se filtra: una con Stock 0 sale marcada como no disponible.
      product.Variants
        .OrderBy(x => x.PublicId, StringComparer.Ordinal)
        .Select(x => new ProductVariantResponse(x.PublicId, x.Name, x.Price, x.Stock, x.Stock > 0))
        .ToList(),
      product.Category == null ? null : CategoryMapper.ToResponse(product.Category));
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
