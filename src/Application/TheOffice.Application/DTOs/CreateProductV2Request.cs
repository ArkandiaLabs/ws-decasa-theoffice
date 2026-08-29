namespace TheOffice.Application.DTOs;

public record CreateProductV2Request(
  string PublicId,
  string Name,
  string Description,
  decimal Price,
  int Stock,
  string CategorySlug,
  IReadOnlyList<CreateProductImageRequest> Images,
  IReadOnlyList<CreateProductVariantRequest> Variants
);
