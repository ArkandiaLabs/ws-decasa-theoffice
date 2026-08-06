using Microsoft.EntityFrameworkCore;

using TheOffice.Persistence.Models;
using TheOffice.Persistence.Seeders;

namespace TheOffice.Persistence;

public class TheOfficeDbContext : DbContext
{
  public DbSet<Customer> Customers { get; set; } = null!;
  public DbSet<Category> Categories { get; set; } = null!;
  public DbSet<Product> Products { get; set; } = null!;

  public TheOfficeDbContext(DbContextOptions options) : base(options) { }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    ConfigureProduct(modelBuilder);

    SeedData(modelBuilder);
  }

  private void ConfigureProduct(ModelBuilder modelBuilder)
  {
    // SQLite guarda decimal como TEXT y no soporta comparacion ni ordenamiento en SQL.
    // La conversion a double habilita filtros y ORDER BY por precio; al pasar a SQL Server se retira.
    modelBuilder.Entity<Product>()
      .Property(x => x.Price)
      .HasConversion<double>();

    modelBuilder.Entity<Product>()
      .HasOne(x => x.Category)
      .WithMany(x => x.Products)
      .HasForeignKey(x => x.CategoryId)
      .OnDelete(DeleteBehavior.Restrict);
  }

  private void SeedData(ModelBuilder modelBuilder)
  {
    modelBuilder.SeedCustomersConfig();
    modelBuilder.SeedCategoriesConfig();
    modelBuilder.SeedProductsConfig();
  }
}
