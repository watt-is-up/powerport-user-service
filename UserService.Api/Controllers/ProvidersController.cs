using Microsoft.AspNetCore.Mvc;
using UserService.Application.Providers.RegisterProvider;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/providers")]
public sealed class ProvidersController : ControllerBase
{
    private readonly IProviderProvisioningService _svc;

    public ProvidersController(IProviderProvisioningService svc) => _svc = svc;

    [HttpPost]
    public async Task<ActionResult<RegisterProviderResult>> Create(
        [FromBody] RegisterProviderRequest request,
        CancellationToken ct)
    {
        var result = await _svc.RegisterProviderAsync(request, ct);
        return Created($"/api/providers/{result.DisplayName}", result);
    }
}

// [TODO] - Make a copy of this controller for Users