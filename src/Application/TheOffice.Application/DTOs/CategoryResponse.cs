namespace TheOffice.Application.DTOs;

public record CategoryResponse(
  string PublicId,
  string Name,
  string Slug,
  string Description
);
