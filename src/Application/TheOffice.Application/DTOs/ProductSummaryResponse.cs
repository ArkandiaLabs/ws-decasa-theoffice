namespace TheOffice.Application.DTOs;

public record ProductSummaryResponse(
  string PublicId,
  string Name,
  decimal Price,
  string ImageUrl,
  int Stock,
  string CategoryName,
  string CategorySlug
);
