using Microsoft.EntityFrameworkCore;

using TheOffice.Persistence.Models;

namespace TheOffice.Persistence.Seeders;

public static class ProductImageSeeder
{
  // Backfill de ARKWS-46: la foto que los 16 productos ya tenian pasa a ser su galeria.
  // La URL se deriva del nombre con la misma expresion que usaba Product.ImageUrl, para que
  // GET /api/v1/products siga devolviendo exactamente el mismo valor que antes del cambio.
  public static void SeedProductImagesConfig(this ModelBuilder modelBuilder)
  {
    var images = ProductSeeder.SeededProducts
      .Select((product, position) => Build(position + 1, product))
      .ToArray();

    modelBuilder.Entity<ProductImage>().HasData(images);
  }

  private static ProductImage Build(int index, Product product)
  {
    return new ProductImage
    {
      // Serie propia: nunca colisiona con la de Product (b0000000-...).
      Id = Guid.Parse($"b1000000-0000-4000-8000-{index:D12}"),
      PublicId = $"{product.PublicId}-IMG-1",
      Url = $"https://placehold.co/600x400/png?text={Uri.EscapeDataString(product.Name)}",
      SortOrder = 0,
      IsPrimary = true,
      ProductId = product.Id
    };
  }
}
