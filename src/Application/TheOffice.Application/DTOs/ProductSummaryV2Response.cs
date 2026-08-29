namespace TheOffice.Application.DTOs;

public record ProductSummaryV2Response(
  string PublicId,
  string Name,
  decimal Price,
  ProductImageResponse? PrimaryImage,
  int Stock,
  bool IsAvailable,
  int VariantCount,
  string CategoryName,
  string CategorySlug
);
