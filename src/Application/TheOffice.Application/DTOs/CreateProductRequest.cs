namespace TheOffice.Application.DTOs;

public record CreateProductRequest(
  string PublicId,
  string Name,
  string Description,
  decimal Price,
  string ImageUrl,
  int Stock,
  string CategorySlug
);
