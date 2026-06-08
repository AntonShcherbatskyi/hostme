using System.IO.Compression;
using HostMe.Domain.Entities;
using HostMe.Domain.Services;
using HostMe.Persistance;
using HostMe.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HostMe.Application.Tests;

public class SiteServiceTests : IDisposable
{
    private readonly HostMeDbContext _dbContext;
    private readonly SiteRepository _siteRepository;
    private readonly UserRepository _userRepository;
    private readonly IS3Service _s3Service;
    private readonly ITempDirectoryFactory _tempDirectoryFactory;
    private readonly SiteService _siteService;

    public SiteServiceTests()
    {
        var options = new DbContextOptionsBuilder<HostMeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HostMeDbContext(options);
        _siteRepository = new SiteRepository(_dbContext);
        _userRepository = new UserRepository(_dbContext);
        _s3Service = Substitute.For<IS3Service>();
        
        _tempDirectoryFactory = new TempDirectoryFactory(); 

        _siteService = new SiteService(
            _siteRepository,
            _userRepository,
            _s3Service,
            _tempDirectoryFactory);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task UploadSiteAsync_WithValidZip_ExtractsCleansMacOsMetadataAndSavesToDb()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "TestUser@Example.Com",
            PasswordHash = "hashed"
        };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var zipFiles = new Dictionary<string, string>
        {
            { "site-folder/index.html", "<h1>Hello Clean World</h1>" },
            { "site-folder/.DS_Store", "ds store content" },
            { "site-folder/._metadata", "apple double content" },
            { "__MACOSX/site-folder/index.html", "mac metadata" }
        };

        using var zipStream = CreateTestZipStream(zipFiles);
        _s3Service.GetSiteUrl(Arg.Any<string>()).Returns("http://fake-s3.com/site");

        var result = await _siteService.UploadSiteAsync(user.Id, "My New Site", zipStream);

        Assert.NotNull(result);
        Assert.Equal("My New Site", result.Name);
        Assert.Equal("http://fake-s3.com/site", result.Url);

        var expectedS3Key = "sites/testuser@example.com/my-new-site";
        await _s3Service.Received(1).UploadFolderAsync(
            Arg.Any<string>(), 
            expectedS3Key, 
            Arg.Any<CancellationToken>());

        var siteInDb = await _dbContext.Sites.FirstOrDefaultAsync(s => s.Id == result.Id);
        Assert.NotNull(siteInDb);
        Assert.Equal(user.Id, siteInDb.UserId);
        Assert.Equal("My New Site", siteInDb.Name);
        Assert.Equal(expectedS3Key, siteInDb.S3Key);
    }

    [Fact]
    public async Task UploadSiteAsync_WithUserNotFound_ThrowsArgumentException()
    {
        using var zipStream = CreateTestZipStream(new Dictionary<string, string> { { "index.html", "test" } });

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _siteService.UploadSiteAsync(Guid.NewGuid(), "SiteName", zipStream));
        Assert.Equal("User not found.", exception.Message);
    }

    [Fact]
    public async Task UploadSiteAsync_WithZipSlip_ThrowsInvalidOperationException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "testuser@example.com",
            PasswordHash = "hashed"
        };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var zipFiles = new Dictionary<string, string>
        {
            { "../../../../outside.txt", "payload" }
        };
        using var zipStream = CreateTestZipStream(zipFiles);

        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _siteService.UploadSiteAsync(user.Id, "SiteName", zipStream));
    }

    [Fact]
    public async Task GetUserSitesAsync_ReturnsCorrectMappedDtos()
    {
        var userId = Guid.NewGuid();
        var site1 = new Site { Id = Guid.NewGuid(), UserId = userId, Name = "Site 1", S3Key = "key1", Url = "url1", CreatedAt = DateTime.UtcNow };
        var site2 = new Site { Id = Guid.NewGuid(), UserId = userId, Name = "Site 2", S3Key = "key2", Url = "url2", CreatedAt = DateTime.UtcNow };
        var siteOther = new Site { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Other Site", S3Key = "key3", Url = "url3", CreatedAt = DateTime.UtcNow };

        await _dbContext.Sites.AddRangeAsync(site1, site2, siteOther);
        await _dbContext.SaveChangesAsync();

        var result = await _siteService.GetUserSitesAsync(userId);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Name == "Site 1" && s.Id == site1.Id);
        Assert.Contains(result, s => s.Name == "Site 2" && s.Id == site2.Id);
        Assert.DoesNotContain(result, s => s.Name == "Other Site");
    }

    [Fact]
    public async Task GetUserSitesAsync_ForUserWithNoSites_ReturnsEmptyList()
    {
        var result = await _siteService.GetUserSitesAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task DeleteSiteAsync_WithValidOwner_DeletesFromS3AndDb()
    {
        var userId = Guid.NewGuid();
        var site = new Site
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "To Delete",
            S3Key = "sites/user/todelete",
            Url = "http://fake-s3.com/todelete",
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Sites.AddAsync(site);
        await _dbContext.SaveChangesAsync();

        await _siteService.DeleteSiteAsync(userId, site.Id);

        await _s3Service.Received(1).DeleteFolderAsync(site.S3Key, Arg.Any<CancellationToken>());
        
        var siteInDb = await _dbContext.Sites.FindAsync(site.Id);
        Assert.Null(siteInDb);
    }

    [Fact]
    public async Task DeleteSiteAsync_WithNonExistentSite_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _siteService.DeleteSiteAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteSiteAsync_WithUnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        var site = new Site
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Owner's Site",
            S3Key = "sites/owner/site",
            Url = "http://fake-s3.com/site",
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Sites.AddAsync(site);
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _siteService.DeleteSiteAsync(Guid.NewGuid(), site.Id));
    }

    private static Stream CreateTestZipStream(Dictionary<string, string> files)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Key);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(file.Value);
            }
        }
        ms.Position = 0;
        return ms;
    }
}
