using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

using TheOffice.Domain.Enums;

namespace TheOffice.Persistence.Models;

[Table("Customers")]
[Index(nameof(PublicId), IsUnique = true)]
public class Customer : BaseModel
{
  [Required]
  [StringLength(50)]
  public string PublicId { get; set; } = null!;

  [Required]
  [StringLength(100)]
  public string Name { get; set; } = null!;

  [Required]
  [EmailAddress]
  [StringLength(255)]
  public string Email { get; set; } = null!;

  [Required]
  public CustomerSource Source { get; set; }
}
