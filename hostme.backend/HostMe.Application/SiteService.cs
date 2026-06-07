using HostMe.Domain.Entities;
using HostMe.Domain.Repositories;
using HostMe.Domain.Services;
using HostMe.Domain.Services.Models;
using System.IO.Compression;

namespace HostMe.Application;

public class SiteService : ISiteService
{
    private readonly ISiteRepository _siteRepository;
    private readonly IS3Service _s3Service;

    public SiteService(ISiteRepository siteRepository, IS3Service s3Service)
    {
        _siteRepository = siteRepository;
        _s3Service = s3Service;
    }

    public async Task<SiteDto> UploadSiteAsync(Guid userId, string name, Stream zipStream, CancellationToken cancellationToken = default)
    {
        var tempZipPath = Path.GetTempFileName();
        var tempExtractPath = Path.Combine(Path.GetTempPath(), "hostme_extract_" + Guid.NewGuid());
        Directory.CreateDirectory(tempExtractPath);

        try
        {
            using (var fileStream = new FileStream(tempZipPath, FileMode.Create))
            {
                await zipStream.CopyToAsync(fileStream, cancellationToken);
            }

            ZipFile.ExtractToDirectory(tempZipPath, tempExtractPath);

            var siteId = Guid.NewGuid();
            var s3Key = $"sites/{userId}/{siteId}";
            var url = _s3Service.GetSiteUrl(s3Key);

            await _s3Service.UploadFolderAsync(tempExtractPath, s3Key, cancellationToken);

            var site = new Site
            {
                Id = siteId,
                UserId = userId,
                Name = name.Trim(),
                S3Key = s3Key,
                Url = url,
                CreatedAt = DateTime.UtcNow
            };

            await _siteRepository.AddAsync(site, cancellationToken);
            await _siteRepository.SaveChangesAsync(cancellationToken);

            return new SiteDto(site.Id, site.UserId, site.Name, site.Url, site.CreatedAt);
        }
        finally
        {
            try
            {
                if (File.Exists(tempZipPath))
                {
                    File.Delete(tempZipPath);
                }
                if (Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, recursive: true);
                }
            }
            catch
            {
                // Silence cleanup errors
            }
        }
    }
}
