namespace TheOffice.Application.DTOs;

public record CreateProductVariantRequest(
  string PublicId,
  string Name,
  decimal Price,
  int Stock
);
