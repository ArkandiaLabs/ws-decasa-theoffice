using Microsoft.EntityFrameworkCore;

using TheOffice.Persistence.Models;

namespace TheOffice.Persistence.Seeders;

public static class ProductSeeder
{
  public static void SeedProductsConfig(this ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Product>().HasData(
      // Papeleria
      Build(1, "PRD-001", "Resma de papel carta 75g", "Resma de 500 hojas tamano carta, 75 gramos, blancura 96%.", 18900m, 120, CategorySeeder.PapeleriaId),
      Build(2, "PRD-002", "Cuaderno argollado 100 hojas", "Cuaderno argollado tamano carta, 100 hojas cuadriculadas, pasta dura.", 12500m, 200, CategorySeeder.PapeleriaId),
      Build(3, "PRD-003", "Boligrafo tinta negra x12", "Caja por 12 boligrafos de tinta negra, punta media de 1.0 mm.", 9800m, 350, CategorySeeder.PapeleriaId),
      Build(4, "PRD-004", "Marcador borrable x4", "Set de 4 marcadores borrables en seco para tablero acrilico.", 15400m, 90, CategorySeeder.PapeleriaId),

      // Mobiliario
      Build(5, "PRD-005", "Silla ergonomica con soporte lumbar", "Silla de malla transpirable con soporte lumbar ajustable y apoyabrazos 3D.", 689000m, 25, CategorySeeder.MobiliarioId),
      Build(6, "PRD-006", "Escritorio en L 150 cm", "Escritorio en L de 150 x 120 cm en madera laminada con pasacables.", 899000m, 12, CategorySeeder.MobiliarioId),
      Build(7, "PRD-007", "Archivador metalico 4 gavetas", "Archivador metalico vertical de 4 gavetas con cerradura central.", 745000m, 8, CategorySeeder.MobiliarioId),
      Build(8, "PRD-008", "Cajonera rodante 3 gavetas", "Cajonera movil de 3 gavetas con ruedas y freno, acabado grafito.", 389000m, 15, CategorySeeder.MobiliarioId),

      // Tecnologia
      Build(9, "PRD-009", "Monitor 27 pulgadas QHD", "Monitor IPS de 27 pulgadas, resolucion 2560x1440, 75 Hz, HDMI y DisplayPort.", 1250000m, 18, CategorySeeder.TecnologiaId),
      Build(10, "PRD-010", "Teclado mecanico inalambrico", "Teclado mecanico 75%, switches lineales, conexion Bluetooth y USB-C.", 329000m, 40, CategorySeeder.TecnologiaId),
      Build(11, "PRD-011", "Diadema con cancelacion de ruido", "Diadema over-ear con cancelacion activa de ruido y 30 horas de bateria.", 459000m, 30, CategorySeeder.TecnologiaId),
      Build(12, "PRD-012", "Base refrigerante para portatil", "Base con 5 ventiladores silenciosos y altura ajustable en 6 posiciones.", 89900m, 45, CategorySeeder.TecnologiaId),

      // Organizacion
      Build(13, "PRD-013", "Organizador de escritorio 5 compartimentos", "Organizador de escritorio en metal perforado con 5 compartimentos.", 54900m, 75, CategorySeeder.OrganizacionId),
      Build(14, "PRD-014", "Caja de archivo tapa fija x10", "Paquete por 10 cajas de archivo inactivo en carton kraft con tapa fija.", 68000m, 60, CategorySeeder.OrganizacionId),
      Build(15, "PRD-015", "Notas adhesivas x6", "Paquete por 6 blocks de notas adhesivas de 76 x 76 mm en colores neon.", 11200m, 180, CategorySeeder.OrganizacionId),
      Build(16, "PRD-016", "Perforadora industrial 30 hojas", "Perforadora de 2 huecos con capacidad para 30 hojas y guia metrica.", 97500m, 22, CategorySeeder.OrganizacionId)
    );
  }

  private static Product Build(int index, string publicId, string name, string description, decimal price, int stock, Guid categoryId)
  {
    return new Product
    {
      Id = Guid.Parse($"b0000000-0000-4000-8000-{index:D12}"),
      PublicId = publicId,
      Name = name,
      Description = description,
      Price = price,
      ImageUrl = $"https://placehold.co/600x400/png?text={Uri.EscapeDataString(name)}",
      Stock = stock,
      IsActive = true,
      CategoryId = categoryId
    };
  }
}
