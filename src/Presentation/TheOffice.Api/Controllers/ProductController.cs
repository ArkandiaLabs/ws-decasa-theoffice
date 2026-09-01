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

  public ProductController(ProductService productService)
  {
    _productService = productService;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] ProductQuery query)
  {
    var result = await _productService.GetAll(query);

    return Ok(result);
  }

  [HttpGet("{publicId}")]
  public async Task<IActionResult> Get(string publicId)
  {
    var result = await _productService.GetByPublicId(publicId);
    if (result == null)
    {
      return NotFound();
    }

    return Ok(result.Value);
  }

  [HttpPost]
  public async Task<IActionResult> Create(CreateProductRequest request)
  {
    var result = await _productService.Create(request);
    if (!result.IsSuccess)
    {
      return BadRequest(result.Error);
    }

    return Ok(result.Value);
  }

  // Descarga la imagen del producto desde su URL y la devuelve en linea, para que el frontend
  // pueda previsualizarla sin exponer el repositorio de medios directamente al navegador.
  [HttpGet("{publicId}/preview")]
  public async Task<IActionResult> Preview(string publicId, [FromQuery] string url)
  {
    using var http = new HttpClient();
    var bytes = await http.GetByteArrayAsync(url);

    return File(bytes, "image/jpeg");
  }
}
