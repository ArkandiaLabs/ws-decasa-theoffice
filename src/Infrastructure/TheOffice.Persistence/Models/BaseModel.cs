using System.ComponentModel.DataAnnotations;

namespace TheOffice.Persistence.Models;

public class BaseModel
{
  [Key]
  public Guid Id { get; set; } = Guid.NewGuid();
}
