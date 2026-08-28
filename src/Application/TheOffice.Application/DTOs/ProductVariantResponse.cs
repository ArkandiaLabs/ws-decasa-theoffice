namespace TheOffice.Application.DTOs;

// IsAvailable es la marca de "agotada" del punto 3 del requerimiento: la presentacion se
// devuelve igual, marcada, para que el cliente sepa que ese color existe.
public record ProductVariantResponse(
  string PublicId,
  string Name,
  decimal Price,
  int Stock,
  bool IsAvailable
);
