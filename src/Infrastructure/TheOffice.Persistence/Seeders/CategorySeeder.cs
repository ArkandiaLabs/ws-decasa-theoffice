using Microsoft.EntityFrameworkCore;

using TheOffice.Persistence.Models;

namespace TheOffice.Persistence.Seeders;

public static class CategorySeeder
{
  public static readonly Guid PapeleriaId = Guid.Parse("c0000000-0000-4000-8000-000000000001");
  public static readonly Guid MobiliarioId = Guid.Parse("c0000000-0000-4000-8000-000000000002");
  public static readonly Guid TecnologiaId = Guid.Parse("c0000000-0000-4000-8000-000000000003");
  public static readonly Guid OrganizacionId = Guid.Parse("c0000000-0000-4000-8000-000000000004");

  public static void SeedCategoriesConfig(this ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Category>().HasData(
      new Category
      {
        Id = PapeleriaId,
        PublicId = "CAT-001",
        Name = "Papeleria",
        Slug = "papeleria",
        Description = "Papel, cuadernos y utiles de escritura para el dia a dia de la oficina."
      },
      new Category
      {
        Id = MobiliarioId,
        PublicId = "CAT-002",
        Name = "Mobiliario",
        Slug = "mobiliario",
        Description = "Sillas, escritorios y archivadores para espacios de trabajo."
      },
      new Category
      {
        Id = TecnologiaId,
        PublicId = "CAT-003",
        Name = "Tecnologia",
        Slug = "tecnologia",
        Description = "Monitores, perifericos y accesorios para puestos de trabajo digitales."
      },
      new Category
      {
        Id = OrganizacionId,
        PublicId = "CAT-004",
        Name = "Organizacion",
        Slug = "organizacion",
        Description = "Archivo, clasificacion y orden del puesto de trabajo."
      }
    );
  }
}
