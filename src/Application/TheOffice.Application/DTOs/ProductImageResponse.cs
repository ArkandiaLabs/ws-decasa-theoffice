namespace TheOffice.Application.DTOs;

public record ProductImageResponse(
  string PublicId,
  string Url,
  int SortOrder,
  bool IsPrimary
);
