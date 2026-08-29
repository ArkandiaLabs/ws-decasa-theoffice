using Microsoft.EntityFrameworkCore;

using TheOffice.Persistence.Models;

namespace TheOffice.Persistence.Seeders;

public static class ProductImageSeeder
{
  // Backfill de ARKWS-46: la foto que los 16 productos ya tenian encabeza ahora su galeria.
  public static void SeedProductImagesConfig(this ModelBuilder modelBuilder)
  {
    var images = ProductSeeder.SeededProducts
      .SelectMany((product, position) => BuildGallery(position + 1, product))
      .ToArray();

    modelBuilder.Entity<ProductImage>().HasData(images);
  }

  // Los angulos que siguen a la principal: una galeria no puede pedir mas de los que hay aqui.
  private static readonly string[] ExtraShots = { "detalle", "en uso", "empaque" };

  private const int DefaultGallerySize = 3;

  // Las excepciones al tamano normal, para que la UI encuentre los dos bordes desde el arranque.
  private static readonly Dictionary<string, int> GallerySizes = new()
  {
    ["PRD-003"] = 1,  // Boligrafo tinta negra x12 -- consumible, una sola toma
    ["PRD-015"] = 1,  // Notas adhesivas x6 -- idem
    ["PRD-005"] = 4,  // Silla ergonomica -- el producto estrella del catalogo
    ["PRD-009"] = 4   // Monitor 27 pulgadas -- idem
  };

  private static IEnumerable<ProductImage> BuildGallery(int position, Product product)
  {
    var size = GallerySizes.TryGetValue(product.PublicId, out var custom)
      ? custom
      : DefaultGallerySize;

    for (var number = 1; number <= size; number++)
    {
      yield return Build(position, number, product);
    }
  }

  private static ProductImage Build(int position, int number, Product product)
  {
    // Bloques de 100 sobre la serie ya sembrada: la foto 1 conserva su Id y numerar de corrido lo pisaria.
    var index = position + ((number - 1) * 100);

    // La foto 1 conserva la URL de antes, para que GET /api/v1/products devuelva la misma imageUrl.
    var label = number == 1 ? product.Name : $"{product.Name} - {ExtraShots[number - 2]}";

    return new ProductImage
    {
      // Serie propia: nunca colisiona con la de Product (b0000000-...).
      Id = Guid.Parse($"b1000000-0000-4000-8000-{index:D12}"),
      PublicId = $"{product.PublicId}-IMG-{number}",
      Url = $"https://placehold.co/600x400/png?text={Uri.EscapeDataString(label)}",
      // El repositorio ordena por IsPrimary, luego SortOrder, luego PublicId.
      SortOrder = number - 1,
      IsPrimary = number == 1,
      ProductId = product.Id
    };
  }
}
