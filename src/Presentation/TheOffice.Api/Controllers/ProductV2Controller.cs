using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using TheOffice.Application.DTOs;
using TheOffice.Application.Services;

namespace TheOffice.Api.Controllers;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/products")]
public class ProductV2Controller : ControllerBase
{
  private readonly ProductService _productService;
  private readonly ILogger<ProductV2Controller> _logger;

  public ProductV2Controller(ProductService productService, ILogger<ProductV2Controller> logger)
  {
    _productService = productService;
    _logger = logger;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] ProductQuery query)
  {
    _logger.LogInformation(
      "GET products v2: page {Page}, pageSize {PageSize}, category {Category}, search {Search}",
      query.Page,
      query.PageSize,
      query.Category,
      query.Search);

    var result = await _productService.GetAllV2(query);

    _logger.LogInformation(
      "GET products v2: returned {Count} of {TotalItems} products",
      result.Items.Count,
      result.TotalItems);

    return Ok(result);
  }

  [HttpGet("{publicId}")]
  public async Task<IActionResult> Get(string publicId)
  {
    _logger.LogInformation("GET product v2 {PublicId}", publicId);

    var result = await _productService.GetByPublicIdV2(publicId);
    if (result == null)
    {
      _logger.LogWarning("GET product v2 {PublicId}: not found", publicId);

      return NotFound();
    }

    _logger.LogInformation("GET product v2 {PublicId}: found", publicId);

    return Ok(result.Value);
  }

  [HttpPost]
  public async Task<IActionResult> Create(CreateProductV2Request request)
  {
    _logger.LogInformation("POST product v2: creating product {PublicId}", request.PublicId);

    var result = await _productService.CreateV2(request);
    if (!result.IsSuccess)
    {
      _logger.LogWarning("POST product v2: rejected, {Error}", result.Error);

      return BadRequest(result.Error);
    }

    _logger.LogInformation("POST product v2: created {PublicId}", result.Value!.PublicId);

    return Ok(result.Value);
  }
}
