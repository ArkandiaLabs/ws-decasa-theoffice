namespace TheOffice.Application.DTOs;

public record ProductResponse(
  string PublicId,
  string Name,
  string Description,
  decimal Price,
  string ImageUrl,
  int Stock,
  bool IsActive,
  CategoryResponse? Category
);
