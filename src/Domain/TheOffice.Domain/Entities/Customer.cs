using TheOffice.Domain.Enums;

namespace TheOffice.Domain.Entities;

public class Customer
{
  public Guid Id { get; set; }
  public string PublicId { get; set; } = null!;
  public string Name { get; set; } = null!;
  public string Email { get; set; } = null!;
  public CustomerSource Source { get; set; }
}
