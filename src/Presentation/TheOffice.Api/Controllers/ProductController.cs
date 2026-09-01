using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using TheOffice.Application.DTOs;
using TheOffice.Application.Services;

namespace TheOffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public class ProductController : ControllerBase
{
  private readonly ProductService _productService;
  private readonly ILogger<ProductController> _logger;

  public ProductController(ProductService productService, ILogger<ProductController> logger)
  {
    _productService = productService;
    _logger = logger;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] ProductQuery query)
  {
    _logger.LogInformation(
      "GET products: page {Page}, pageSize {PageSize}, category {Category}, search {Search}",
      query.Page,
      query.PageSize,
      query.Category,
      query.Search);

    var result = await _productService.GetAll(query);

    _logger.LogInformation(
      "GET products: returned {Count} of {TotalItems} products",
      result.Items.Count,
      result.TotalItems);

    return Ok(result);
  }

  [HttpGet("{publicId}")]
  public async Task<IActionResult> Get(string publicId)
  {
    _logger.LogInformation("GET product {PublicId}", publicId);

    var result = await _productService.GetByPublicId(publicId);
    if (result == null)
    {
      _logger.LogWarning("GET product {PublicId}: not found", publicId);

      return NotFound();
    }

    _logger.LogInformation("GET product {PublicId}: found", publicId);

    return Ok(result.Value);
  }

  [HttpPost]
  public async Task<IActionResult> Create(CreateProductRequest request)
  {
    _logger.LogInformation("POST product: creating product {PublicId}", request.PublicId);

    var result = await _productService.Create(request);
    if (!result.IsSuccess)
    {
      _logger.LogWarning("POST product: rejected, {Error}", result.Error);

      return BadRequest(result.Error);
    }

    _logger.LogInformation("POST product: created {PublicId}", result.Value!.PublicId);

    return Ok(result.Value);
  }

  // Descarga la imagen del producto desde su URL y la devuelve en linea, para que el frontend
  // pueda previsualizarla sin exponer el repositorio de medios directamente al navegador.
  [HttpGet("{publicId}/preview")]
  public async Task<IActionResult> Preview(string publicId, [FromQuery] string url)
  {
    _logger.LogInformation("GET product {PublicId} preview: fetching {Url}", publicId, url);

    using var http = new HttpClient();
    var bytes = await http.GetByteArrayAsync(url);

    _logger.LogInformation(
      "GET product {PublicId} preview: fetched {Bytes} bytes",
      publicId,
      bytes.Length);

    return File(bytes, "image/jpeg");
  }
}
