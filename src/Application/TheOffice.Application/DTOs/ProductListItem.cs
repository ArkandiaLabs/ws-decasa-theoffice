namespace TheOffice.Application.DTOs;

// Lo que el listado necesita, y nada mas. El repositorio lo proyecta en SQL en vez de
// materializar el grafo completo: traer la galeria entera de cada producto multiplicaria
// las filas del JOIN, y el listado solo muestra la foto principal.
public record ProductListItem(
  string PublicId,
  string Name,
  decimal Price,
  int Stock,
  string CategoryName,
  string CategorySlug,
  ProductImageResponse? PrimaryImage,
  int VariantCount
);
