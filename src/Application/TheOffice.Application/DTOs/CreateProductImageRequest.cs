namespace TheOffice.Application.DTOs;

// El PublicId lo genera el servidor a partir del producto: las fotos no se capturan con
// identificador propio. Son ligas al repositorio de medios, no archivos que se suban aqui.
public record CreateProductImageRequest(
  string Url,
  int SortOrder,
  bool IsPrimary
);
