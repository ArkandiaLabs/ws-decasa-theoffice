using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace TheOffice.Persistence.Models;

[Table("ProductImages")]
[Index(nameof(PublicId), IsUnique = true)]
public class ProductImage : BaseModel
{
  [Required]
  [StringLength(50)]
  public string PublicId { get; set; } = null!;

  [Required]
  [StringLength(500)]
  public string Url { get; set; } = null!;

  [Required]
  public int SortOrder { get; set; }

  [Required]
  public bool IsPrimary { get; set; }

  [Required]
  public Guid ProductId { get; set; }

  public Product Product { get; set; } = null!;
}
