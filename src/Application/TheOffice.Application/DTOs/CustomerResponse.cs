namespace TheOffice.Application.DTOs;

public record CustomerResponse(
  string PublicId,
  string Name,
  string Email,
  string Source
);
