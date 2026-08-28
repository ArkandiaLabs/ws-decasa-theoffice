namespace TheOffice.Application.DTOs;

public record ProductV2Response(
  string PublicId,
  string Name,
  string Description,
  decimal Price,
  int Stock,
  bool IsActive,
  IReadOnlyList<ProductImageResponse> Images,
  IReadOnlyList<ProductVariantResponse> Variants,
  CategoryResponse? Category
);
