using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace TheOffice.Persistence.Models;

[Table("Categories")]
[Index(nameof(PublicId), IsUnique = true)]
[Index(nameof(Slug), IsUnique = true)]
public class Category : BaseModel
{
  [Required]
  [StringLength(50)]
  public string PublicId { get; set; } = null!;

  [Required]
  [StringLength(100)]
  public string Name { get; set; } = null!;

  [Required]
  [StringLength(100)]
  public string Slug { get; set; } = null!;

  [Required]
  [StringLength(500)]
  public string Description { get; set; } = null!;

  public ICollection<Product> Products { get; set; } = new List<Product>();
}
