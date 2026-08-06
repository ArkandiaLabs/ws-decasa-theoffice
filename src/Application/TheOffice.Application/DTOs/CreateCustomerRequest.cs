namespace TheOffice.Application.DTOs;

public record CreateCustomerRequest(
  string PublicId,
  string Name,
  string Email,
  string Source
);
