namespace TheOffice.Application.DTOs;

// Clase y no record: el model binding de [FromQuery] necesita constructor sin parametros.
public class ProductQuery
{
  public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 6;
  public string? Category { get; set; }
  public string? Search { get; set; }
}
