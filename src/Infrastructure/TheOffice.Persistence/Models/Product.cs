using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace TheOffice.Persistence.Models;

[Table("Products")]
[Index(nameof(PublicId), IsUnique = true)]
public class Product : BaseModel
{
  [Required]
  [StringLength(50)]
  public string PublicId { get; set; } = null!;

  [Required]
  [StringLength(150)]
  public string Name { get; set; } = null!;

  [Required]
  [StringLength(1000)]
  public string Description { get; set; } = null!;

  [Required]
  public decimal Price { get; set; }

  [Required]
  [StringLength(500)]
  public string ImageUrl { get; set; } = null!;

  [Required]
  public int Stock { get; set; }

  [Required]
  public bool IsActive { get; set; } = true;

  [Required]
  public Guid CategoryId { get; set; }

  public Category Category { get; set; } = null!;
}
