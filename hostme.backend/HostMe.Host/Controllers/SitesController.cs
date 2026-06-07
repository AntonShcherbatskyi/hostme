using System.Security.Claims;
using HostMe.Domain.Services;
using HostMe.Domain.Services.Models;
using HostMe.Host.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HostMe.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SitesController : ControllerBase
{
    private readonly ISiteService _siteService;

    public SitesController(ISiteService siteService)
    {
        _siteService = siteService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadSiteRequest request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Failure("ZIP file is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<object>.Failure("Site name is required."));
        }

        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (extension != ".zip")
        {
            return BadRequest(ApiResponse<object>.Failure("Only ZIP files are supported."));
        }

        Guid userId;
        try
        {
            userId = GetUserId();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Failure(ex.Message));
        }

        using var stream = request.File.OpenReadStream();
        var result = await _siteService.UploadSiteAsync(userId, request.Name, stream, cancellationToken);

        var response = new SiteResponse(result.Id, result.Name, result.Url, result.CreatedAt);
        return Ok(ApiResponse<SiteResponse>.Success(response));
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated or user claim is invalid.");
        }

        return userId;
    }
}
