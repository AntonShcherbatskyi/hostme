using HostMe.Domain.Constants;
using HostMe.Domain.Entities;
using HostMe.Domain.Repositories;
using HostMe.Domain.Services;
using HostMe.Domain.Services.Models;

namespace HostMe.Application;

public class SiteService : ISiteService
{
    private readonly ISiteRepository _siteRepository;
    private readonly IUserRepository _userRepository;
    private readonly IS3Service _s3Service;
    private readonly ITempDirectoryFactory _tempDirectoryFactory;

    public SiteService(
        ISiteRepository siteRepository,
        IUserRepository userRepository,
        IS3Service s3Service,
        ITempDirectoryFactory tempDirectoryFactory)
    {
        _siteRepository = siteRepository;
        _userRepository = userRepository;
        _s3Service = s3Service;
        _tempDirectoryFactory = tempDirectoryFactory;
    }

    public async Task<SiteDto> UploadSiteAsync(
        Guid userId, string name, Stream zipStream, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new ArgumentException(ErrorMessages.Site.UserNotFound);

        using var tempDir = _tempDirectoryFactory.Create();

        ZipExtractor.Extract(zipStream, tempDir.Path);
        ZipExtractor.RemoveMacOsMetadata(tempDir.Path);
        var uploadDir = ZipExtractor.ResolveUploadRoot(tempDir.Path);

        SiteFileValidator.Validate(uploadDir);

        var slug    = SlugHelper.Slugify(name);
        var email   = user.Email.ToLowerInvariant().Trim();
        var s3Key   = $"sites/{email}/{slug}";
        var url     = _s3Service.GetSiteUrl(s3Key);

        await _s3Service.UploadFolderAsync(uploadDir, s3Key, cancellationToken);

        var site = new Site
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            Name      = name.Trim(),
            S3Key     = s3Key,
            Url       = url,
            CreatedAt = DateTime.UtcNow
        };

        await _siteRepository.AddAsync(site, cancellationToken);
        await _siteRepository.SaveChangesAsync(cancellationToken);

        return ToDto(site);
    }

    public async Task<IReadOnlyList<SiteDto>> GetUserSitesAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var sites = await _siteRepository.GetByUserIdAsync(userId, cancellationToken);
        return sites.Select(ToDto).ToList();
    }

    public async Task DeleteSiteAsync(
        Guid userId, Guid siteId, CancellationToken cancellationToken = default)
    {
        var site = await _siteRepository.GetByIdAsync(siteId, cancellationToken)
            ?? throw new KeyNotFoundException(ErrorMessages.Site.NotFound);

        if (site.UserId != userId)
            throw new UnauthorizedAccessException(ErrorMessages.Site.Forbidden);

        await _s3Service.DeleteFolderAsync(site.S3Key, cancellationToken);

        _siteRepository.Delete(site);
        await _siteRepository.SaveChangesAsync(cancellationToken);
    }
    
    private static SiteDto ToDto(Site site) =>
        new(site.Id, site.UserId, site.Name, site.Url, site.CreatedAt);
}