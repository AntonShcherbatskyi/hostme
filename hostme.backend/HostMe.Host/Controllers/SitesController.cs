using System.Net.Mime;
using HostMe.Domain.Constants;
using HostMe.Domain.Services;
using HostMe.Domain.Services.Models;
using HostMe.Host.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HostMe.Host.Controllers;

[Route("api/[controller]")]
[Authorize]
public class SitesController : ApiControllerBase
{
    private readonly ISiteService _siteService;

    public SitesController(ISiteService siteService)
    {
        _siteService = siteService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm] UploadSiteRequest request, CancellationToken cancellationToken)
    {
        var extension   = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        var contentType = request.File.ContentType;

        if (extension != ".zip" || contentType != MediaTypeNames.Application.Zip)
            return BadRequest(ApiResponse.Failure(ErrorMessages.Site.ContentTypeZIPSupported));

        if (TryGetCurrentUserId() is not { } userId)
            return Unauthorized();

        using var stream = request.File.OpenReadStream();
        var result = await _siteService.UploadSiteAsync(userId, request.Name, stream, cancellationToken);

        return Ok(ApiResponse<SiteResponse>.Success(new SiteResponse(result.Id, result.Name, result.Url, result.CreatedAt)));
    }

    [HttpGet]
    public async Task<IActionResult> GetSites(CancellationToken cancellationToken)
    {
        if (TryGetCurrentUserId() is not { } userId)
            return Unauthorized();

        var sites    = await _siteService.GetUserSitesAsync(userId, cancellationToken);
        var response = sites.Select(s => new SiteResponse(s.Id, s.Name, s.Url, s.CreatedAt)).ToList();

        return Ok(ApiResponse<List<SiteResponse>>.Success(response));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (TryGetCurrentUserId() is not { } userId)
            return Unauthorized();

        await _siteService.DeleteSiteAsync(userId, id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }
}
