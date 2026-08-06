namespace TheOffice.Domain.Entities;

public class Category
{
  public Guid Id { get; set; }
  public string PublicId { get; set; } = null!;
  public string Name { get; set; } = null!;
  public string Slug { get; set; } = null!;
  public string Description { get; set; } = null!;
}
