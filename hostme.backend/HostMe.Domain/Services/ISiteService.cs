using HostMe.Domain.Services.Models;

namespace HostMe.Domain.Services;

public interface ISiteService
{
    Task<SiteDto> UploadSiteAsync(Guid userId, string name, Stream zipStream, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteDto>> GetUserSitesAsync(Guid userId, CancellationToken cancellationToken = default);
}
