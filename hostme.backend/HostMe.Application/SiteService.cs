using HostMe.Domain.Entities;
using HostMe.Domain.Repositories;
using HostMe.Domain.Services;
using HostMe.Domain.Services.Models;
using System.IO.Compression;

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

    public async Task<SiteDto> UploadSiteAsync(Guid userId, string name, Stream zipStream, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            throw new ArgumentException("User not found.");

        using var tempDir = _tempDirectoryFactory.Create();

        ExtractZip(zipStream, tempDir.Path);
        CleanMacOsMetadata(tempDir.Path);
        var uploadDir = DetermineUploadDir(tempDir.Path);

        var siteNameSlug = Slugify(name);
        var userEmail = user.Email.ToLowerInvariant().Trim();
        var s3Key = $"sites/{userEmail}/{siteNameSlug}";
        var url = _s3Service.GetSiteUrl(s3Key);

        await _s3Service.UploadFolderAsync(uploadDir, s3Key, cancellationToken);

        var site = new Site
        {
            Id = Guid.NewGuid(),
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

    public async Task<IReadOnlyList<SiteDto>> GetUserSitesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sites = await _siteRepository.GetByUserIdAsync(userId, cancellationToken);
        return sites.Select(s => new SiteDto(s.Id, s.UserId, s.Name, s.Url, s.CreatedAt)).ToList();
    }

    private static void ExtractZip(Stream zipStream, string destinationDir)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;

            var fullPath = Path.GetFullPath(Path.Combine(destinationDir, entry.FullName));

            if (!fullPath.StartsWith(destinationDir, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"ZipSlip detected: {entry.FullName}");

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            entry.ExtractToFile(fullPath, overwrite: true);
        }
    }

    private static void CleanMacOsMetadata(string dir)
    {
        foreach (var macDir in Directory.GetDirectories(dir, "__MACOSX", SearchOption.AllDirectories))
            try { Directory.Delete(macDir, recursive: true); } catch { }

        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase) || name.StartsWith("._"))
                try { File.Delete(file); } catch { }
        }
    }

    private static string DetermineUploadDir(string baseDir)
    {
        var entries = Directory.GetFileSystemEntries(baseDir);
        return entries.Length == 1 && Directory.Exists(entries[0]) ? entries[0] : baseDir;
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var c in text.ToLowerInvariant().Trim())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c is ' ' or '-' or '_') sb.Append('-');
        }

        var result = sb.ToString().Trim('-');

        while (result.Contains("--"))
            result = result.Replace("--", "-");

        return result;
    }
}