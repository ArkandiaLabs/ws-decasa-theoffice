namespace TheOffice.Domain.Entities;

public class ProductVariant
{
  public Guid Id { get; set; }
  public string PublicId { get; set; } = null!;
  public string Name { get; set; } = null!;
  public decimal Price { get; set; }
  public int Stock { get; set; }
  public Guid ProductId { get; set; }
}
