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

  public CategoryController(CategoryService categoryService)
  {
    _categoryService = categoryService;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    var result = await _categoryService.GetAll();

    return Ok(result);
  }
}
