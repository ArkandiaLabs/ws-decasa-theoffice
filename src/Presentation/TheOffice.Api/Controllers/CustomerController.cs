using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

using TheOffice.Application.Services;
using TheOffice.Application.DTOs;

namespace TheOffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers")]
public class CustomerController : ControllerBase
{
  private readonly CustomerService _customerService;

  public CustomerController(CustomerService customerService)
  {
    _customerService = customerService;
  }

  [HttpPost]
  public async Task<IActionResult> Create(CreateCustomerRequest request)
  {
    var result = await _customerService.Create(request);
    if (!result.IsSuccess)
    {
      return BadRequest(result.Error);
    }

    return Ok(result.Value);
  }

  [HttpGet("{publicId}")]
  public async Task<IActionResult> Get(string publicId)
  {
    var result = await _customerService.GetByPublicId(publicId);
    if (result == null)
    {
      return NotFound();
    }

    return Ok(result.Value);
  }
}
