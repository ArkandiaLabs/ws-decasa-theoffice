using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using TheOffice.Application.Services;

namespace TheOffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/categories")]
public class CategoryController : ControllerBase
{
  private readonly CategoryService _categoryService;
  private readonly ILogger<CategoryController> _logger;

  public CategoryController(CategoryService categoryService, ILogger<CategoryController> logger)
  {
    _categoryService = categoryService;
    _logger = logger;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    _logger.LogInformation("GET categories: listing all categories");

    var result = await _categoryService.GetAll();

    _logger.LogInformation("GET categories: returned {Count} categories", result.Count);

    return Ok(result);
  }
}
