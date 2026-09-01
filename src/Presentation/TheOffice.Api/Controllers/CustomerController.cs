using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using TheOffice.Application.DTOs;
using TheOffice.Application.Services;

namespace TheOffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers")]
public class CustomerController : ControllerBase
{
  private readonly CustomerService _customerService;
  private readonly ILogger<CustomerController> _logger;

  public CustomerController(CustomerService customerService, ILogger<CustomerController> logger)
  {
    _customerService = customerService;
    _logger = logger;
  }

  [HttpPost]
  public async Task<IActionResult> Create(CreateCustomerRequest request)
  {
    // Sin datos del request en el mensaje: nombre y correo son datos personales.
    _logger.LogInformation("POST customer: creating customer");

    var result = await _customerService.Create(request);
    if (!result.IsSuccess)
    {
      _logger.LogWarning("POST customer: rejected, {Error}", result.Error);

      return BadRequest(result.Error);
    }

    _logger.LogInformation("POST customer: created {PublicId}", result.Value!.PublicId);

    return Ok(result.Value);
  }

  [HttpGet("{publicId}")]
  public async Task<IActionResult> Get(string publicId)
  {
    _logger.LogInformation("GET customer {PublicId}", publicId);

    var result = await _customerService.GetByPublicId(publicId);
    if (result == null)
    {
      _logger.LogWarning("GET customer {PublicId}: not found", publicId);

      return NotFound();
    }

    _logger.LogInformation("GET customer {PublicId}: found", publicId);

    return Ok(result.Value);
  }
}
