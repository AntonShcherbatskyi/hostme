using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HostMe.Domain.Services.Models;
using HostMe.Host.Models;
using HostMe.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HostMe.Host.Tests;

public class SitesIntegrationTests : IClassFixture<HostMeWebApplicationFactory>
{
    private readonly HostMeWebApplicationFactory _factory;

    public SitesIntegrationTests(HostMeWebApplicationFactory factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task UploadSite_WithValidZip_ShouldExtractUploadToS3AndSaveToDb()
    {
        var zipPath = CreateZipWithFiles(new Dictionary<string, string>
        {
            { "index.html", "<h1>Hello World</h1>" },
            { "style.css",  "body { color: red; }" }
        });

        var client = _factory.CreateClient();
        var token  = await RegisterAndLoginAsync(client, "siteuser@example.com", "testsiteuser");
        var result = await UploadSiteAsync(client, token, zipPath, "My Cool Static Site");
        File.Delete(zipPath);

        Assert.Equal("My Cool Static Site", result.Name);
        Assert.NotEmpty(result.Url);

        using (var scope = _factory.Services.CreateScope())
        {
            var db   = scope.ServiceProvider.GetRequiredService<HostMeDbContext>();
            var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == result.Id);
            Assert.NotNull(site);
            Assert.Equal("My Cool Static Site", site.Name);
            Assert.Equal(result.Url, site.Url);
        }

        var s3Key          = $"sites/siteuser@example.com/my-cool-static-site";
        var expectedHtml   = Path.Combine(_factory.FakeS3.RootDir, s3Key, "index.html");
        var expectedCss    = Path.Combine(_factory.FakeS3.RootDir, s3Key, "style.css");

        Assert.True(File.Exists(expectedHtml));
        Assert.True(File.Exists(expectedCss));
        Assert.Equal("<h1>Hello World</h1>", File.ReadAllText(expectedHtml));
        Assert.Equal("body { color: red; }", File.ReadAllText(expectedCss));
    }

    [Fact]
    public async Task UploadSite_WithWrapperFolderInZip_ShouldFlattenAndUploadToS3Root()
    {
        var zipPath = CreateZipWithFiles(new Dictionary<string, string>
        {
            { "my-wrapper-dir/index.html", "<h1>Hello Nested World</h1>" }
        });

        var client = _factory.CreateClient();
        var token  = await RegisterAndLoginAsync(client, "siteuser2@example.com", "testsiteuser2");
        var result = await UploadSiteAsync(client, token, zipPath, "Wrapped Site");
        File.Delete(zipPath);

        var s3Key        = $"sites/siteuser2@example.com/wrapped-site";
        var expectedHtml = Path.Combine(_factory.FakeS3.RootDir, s3Key, "index.html");

        Assert.True(File.Exists(expectedHtml));
        Assert.Equal("<h1>Hello Nested World</h1>", File.ReadAllText(expectedHtml));
    }

    [Fact]
    public async Task UploadSite_WithMacMetadataInZip_ShouldFilterMetadataAndUploadOnlyWebFilesToS3Root()
    {
        var zipPath = CreateZipWithFiles(new Dictionary<string, string>
        {
            { "site-folder/index.html",        "<h1>Hello Clean World</h1>" },
            { "site-folder/.DS_Store",         "mock ds_store" },
            { "__MACOSX/site-folder/index.html", "mac metadata" }
        });

        var client = _factory.CreateClient();
        var token  = await RegisterAndLoginAsync(client, "siteuser3@example.com", "testsiteuser3");
        await UploadSiteAsync(client, token, zipPath, "Clean Site");
        File.Delete(zipPath);

        var s3Key          = $"sites/siteuser3@example.com/clean-site";
        var expectedHtml   = Path.Combine(_factory.FakeS3.RootDir, s3Key, "index.html");
        var unexpectedDs   = Path.Combine(_factory.FakeS3.RootDir, s3Key, ".DS_Store");
        var unexpectedMac  = Path.Combine(_factory.FakeS3.RootDir, s3Key, "__MACOSX");

        Assert.True(File.Exists(expectedHtml));
        Assert.False(File.Exists(unexpectedDs));
        Assert.False(Directory.Exists(unexpectedMac));
    }
    
    [Fact]
    public async Task GetSites_ReturnsOnlyCurrentUserSites()
    {
        var zipPath = CreateSinglePageZip();

        var client = _factory.CreateClient();
        var tokenA = await RegisterAndLoginAsync(client, $"usera_{Guid.NewGuid()}@example.com", "usera");
        var tokenB = await RegisterAndLoginAsync(client, $"userb_{Guid.NewGuid()}@example.com", "userb");

        await UploadSiteAsync(client, tokenA, zipPath, "User A Site");
        await UploadSiteAsync(client, tokenB, zipPath, "User B Site");
        File.Delete(zipPath);

        var resultA = await GetSitesAsync(client, tokenA);
        Assert.Single(resultA);
        Assert.Equal("User A Site", resultA[0].Name);

        var resultB = await GetSitesAsync(client, tokenB);
        Assert.Single(resultB);
        Assert.Equal("User B Site", resultB[0].Name);
    }

    [Fact]
    public async Task GetSites_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync("/api/sites");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task DeleteSite_WithValidIdAndOwner_ShouldDeleteFromS3AndDb()
    {
        var zipPath = CreateSinglePageZip();

        var client = _factory.CreateClient();
        var email  = $"deleteuser_{Guid.NewGuid()}@example.com";
        var token  = await RegisterAndLoginAsync(client, email, "deleteuser");
        var site   = await UploadSiteAsync(client, token, zipPath, "ToDeleteSite");
        File.Delete(zipPath);

        var s3Key = $"sites/{email.ToLowerInvariant()}/todeletesite";
        Assert.True(File.Exists(Path.Combine(_factory.FakeS3.RootDir, s3Key, "index.html")));

        var deleteResponse = await AuthorizedDeleteAsync(client, token, $"/api/sites/{site.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HostMeDbContext>();
            Assert.Null(await db.Sites.FindAsync(site.Id));
        }

        Assert.False(Directory.Exists(Path.Combine(_factory.FakeS3.RootDir, s3Key)));
    }

    [Fact]
    public async Task DeleteSite_WithValidIdButNotOwner_ShouldReturnForbidden()
    {
        var zipPath    = CreateSinglePageZip();
        var client     = _factory.CreateClient();
        var ownerToken = await RegisterAndLoginAsync(client, $"owner_{Guid.NewGuid()}@example.com", "owner");
        var thiefToken = await RegisterAndLoginAsync(client, $"thief_{Guid.NewGuid()}@example.com", "thief");

        var site = await UploadSiteAsync(client, ownerToken, zipPath, "OwnerSite");
        File.Delete(zipPath);

        var response = await AuthorizedDeleteAsync(client, thiefToken, $"/api/sites/{site.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSite_WithNonExistentId_ShouldReturnNotFound()
    {
        var client = _factory.CreateClient();
        var token  = await RegisterAndLoginAsync(client, $"user_{Guid.NewGuid()}@example.com", "user");

        var response = await AuthorizedDeleteAsync(client, token, $"/api/sites/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSite_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.DeleteAsync($"/api/sites/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string CreateZipWithFiles(Dictionary<string, string> files)
    {
        var sourceDir = Path.Combine(Path.GetTempPath(), "src_" + Guid.NewGuid());
        Directory.CreateDirectory(sourceDir);

        foreach (var (relativePath, content) in files)
        {
            var fullPath = Path.Combine(sourceDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
        }

        var zipPath = Path.Combine(Path.GetTempPath(), "site_" + Guid.NewGuid() + ".zip");
        ZipFile.CreateFromDirectory(sourceDir, zipPath);
        Directory.Delete(sourceDir, recursive: true);
        return zipPath;
    }

    private static string CreateSinglePageZip() =>
        CreateZipWithFiles(new Dictionary<string, string> { { "index.html", "<h1>Test</h1>" } });

    private static async Task<string> RegisterAndLoginAsync(
        HttpClient client, string email, string username)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = username,
            Email    = email,
            Password = "password123"
        });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email    = email,
            Password = "password123"
        });

        var result = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return result!.Data!.Token;
    }

    private static async Task<SiteResponse> UploadSiteAsync(
        HttpClient client, string token, string zipPath, string siteName)
    {
        using var requestContent = new MultipartFormDataContent();
        using var fileStream     = File.OpenRead(zipPath);
        var streamContent        = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        requestContent.Add(streamContent, "File", "site.zip");
        requestContent.Add(new StringContent(siteName), "Name");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.PostAsync("/api/sites/upload", requestContent);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SiteResponse>>();
        return result!.Data!;
    }

    private static async Task<List<SiteResponse>> GetSitesAsync(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/sites");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SiteResponse>>>();
        return result!.Data!;
    }

    private static async Task<HttpResponseMessage> AuthorizedDeleteAsync(
        HttpClient client, string token, string url)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.DeleteAsync(url);
    }
}
