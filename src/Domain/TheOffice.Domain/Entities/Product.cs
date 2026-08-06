namespace TheOffice.Domain.Entities;

public class Product
{
  public Guid Id { get; set; }
  public string PublicId { get; set; } = null!;
  public string Name { get; set; } = null!;
  public string Description { get; set; } = null!;
  public decimal Price { get; set; }
  public string ImageUrl { get; set; } = null!;
  public int Stock { get; set; }
  public bool IsActive { get; set; }
  public Guid CategoryId { get; set; }
  public Category? Category { get; set; }
}
