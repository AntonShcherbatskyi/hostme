using System.Security.Claims;
using HostMe.Domain.Constants;
using HostMe.Domain.Services.Models;
using HostMe.Host.Constants;
using Microsoft.AspNetCore.Mvc;

namespace HostMe.Host.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid? TryGetCurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst(ClaimConstants.Subject)?.Value;

        return Guid.TryParse(raw, out var id) ? id : null;
    }

    protected new IActionResult Unauthorized() =>
        base.Unauthorized(ApiResponse.Failure(ErrorMessages.User.Unathorized));
}
