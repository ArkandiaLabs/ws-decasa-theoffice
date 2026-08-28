using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace TheOffice.Persistence.Models;

[Table("ProductVariants")]
[Index(nameof(PublicId), IsUnique = true)]
public class ProductVariant : BaseModel
{
  [Required]
  [StringLength(50)]
  public string PublicId { get; set; } = null!;

  [Required]
  [StringLength(100)]
  public string Name { get; set; } = null!;

  [Required]
  public decimal Price { get; set; }

  [Required]
  public int Stock { get; set; }

  [Required]
  public Guid ProductId { get; set; }

  public Product Product { get; set; } = null!;
}
