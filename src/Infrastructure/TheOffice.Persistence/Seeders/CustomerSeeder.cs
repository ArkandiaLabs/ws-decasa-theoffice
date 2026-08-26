using Microsoft.EntityFrameworkCore;

using TheOffice.Domain.Enums;
using TheOffice.Persistence.Models;

namespace TheOffice.Persistence.Seeders;

public static class CustomerSeeder
{
  public static void SeedCustomersConfig(this ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Customer>().HasData(
      new Customer
      {
        Id = Guid.Parse("a0000000-0000-4000-8000-000000000001"),
        PublicId = "CUS-001",
        Name = "Laura Restrepo",
        Email = "laura.restrepo@example.com",
        Source = CustomerSource.Website
      },
      new Customer
      {
        Id = Guid.Parse("a0000000-0000-4000-8000-000000000002"),
        PublicId = "CUS-002",
        Name = "Andres Gomez",
        Email = "andres.gomez@example.com",
        Source = CustomerSource.SocialMedia
      },
      new Customer
      {
        Id = Guid.Parse("a0000000-0000-4000-8000-000000000003"),
        PublicId = "CUS-003",
        Name = "Marcela Rios",
        Email = "marcela.rios@example.com",
        Source = CustomerSource.Email
      }
    );
  }
}
