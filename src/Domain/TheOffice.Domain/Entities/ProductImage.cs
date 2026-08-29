namespace TheOffice.Domain.Entities;

public class ProductImage
{
  public Guid Id { get; set; }
  public string PublicId { get; set; } = null!;
  public string Url { get; set; } = null!;
  public int SortOrder { get; set; }
  public bool IsPrimary { get; set; }
  public Guid ProductId { get; set; }
}
