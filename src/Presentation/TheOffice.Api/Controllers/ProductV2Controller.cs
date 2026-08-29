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

  public ProductV2Controller(ProductService productService)
  {
    _productService = productService;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] ProductQuery query)
  {
    var result = await _productService.GetAllV2(query);

    return Ok(result);
  }

  [HttpGet("{publicId}")]
  public async Task<IActionResult> Get(string publicId)
  {
    var result = await _productService.GetByPublicIdV2(publicId);
    if (result == null)
    {
      return NotFound();
    }

    return Ok(result.Value);
  }

  [HttpPost]
  public async Task<IActionResult> Create(CreateProductV2Request request)
  {
    var result = await _productService.CreateV2(request);
    if (!result.IsSuccess)
    {
      return BadRequest(result.Error);
    }

    return Ok(result.Value);
  }
}
