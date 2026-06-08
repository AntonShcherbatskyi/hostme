using System.Net.Mime;
using System.Security.Claims;
using HostMe.Domain.Constants;
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
        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        var contentType = request.File.ContentType;
        
        if (extension != ".zip" || contentType != MediaTypeNames.Application.Zip)
        {
            return BadRequest(ApiResponse.Failure(ErrorMessages.Site.ContentTypeZIPSupported));
        }
        
        Guid userId;
        try
        {
            userId = GetUserId();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Failure(ex.Message));
        }

        using var stream = request.File.OpenReadStream();
        var result = await _siteService.UploadSiteAsync(userId, request.Name, stream, cancellationToken);

        var response = new SiteResponse(result.Id, result.Name, result.Url, result.CreatedAt);
        return Ok(ApiResponse<SiteResponse>.Success(response));
    }

    [HttpGet]
    public async Task<IActionResult> GetSites(CancellationToken cancellationToken)
    {
        Guid userId;
        try
        {
            userId = GetUserId();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Failure(ex.Message));
        }

        var sites = await _siteService.GetUserSitesAsync(userId, cancellationToken);
        var response = sites.Select(s => new SiteResponse(s.Id, s.Name, s.Url, s.CreatedAt)).ToList();
        return Ok(ApiResponse<List<SiteResponse>>.Success(response));
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            throw new UnauthorizedAccessException(ErrorMessages.User.Unathorized);
        }

        return userId;
    }
}
