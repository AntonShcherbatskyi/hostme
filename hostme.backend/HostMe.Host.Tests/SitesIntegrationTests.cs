using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HostMe.Domain.Services;
using HostMe.Domain.Services.Models;
using HostMe.Host.Models;
using HostMe.Persistance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        var tempSourceDir = Path.Combine(Path.GetTempPath(), "source_" + Guid.NewGuid());
        Directory.CreateDirectory(tempSourceDir);
        File.WriteAllText(Path.Combine(tempSourceDir, "index.html"), "<h1>Hello World</h1>");
        File.WriteAllText(Path.Combine(tempSourceDir, "style.css"), "body { color: red; }");

        var tempZipPath = Path.Combine(Path.GetTempPath(), "site_" + Guid.NewGuid() + ".zip");
        ZipFile.CreateFromDirectory(tempSourceDir, tempZipPath);

        Directory.Delete(tempSourceDir, true);

        var client = _factory.CreateClient();

        var registerRequest = new RegisterRequest
        {
            Username = "testsiteuser",
            Email = "siteuser@example.com",
            Password = "password123"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginRequest = new LoginRequest
        {
            Email = "siteuser@example.com",
            Password = "password123"
        };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.NotNull(loginResult?.Data);
        var token = loginResult.Data.Token;

        using var requestContent = new MultipartFormDataContent();
        var fileStream = File.OpenRead(tempZipPath);
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        requestContent.Add(streamContent, "File", "site.zip");
        requestContent.Add(new StringContent("My Cool Static Site"), "Name");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uploadResponse = await client.PostAsync("/api/sites/upload", requestContent);

        fileStream.Close();
        File.Delete(tempZipPath);

        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<SiteResponse>>();
        Assert.NotNull(uploadResult);
        Assert.False(uploadResult.IsError);
        Assert.NotNull(uploadResult.Data);
        Assert.Equal("My Cool Static Site", uploadResult.Data.Name);
        Assert.NotEmpty(uploadResult.Data.Url);

        var siteId = uploadResult.Data.Id;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HostMeDbContext>();
            var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == siteId);
            Assert.NotNull(site);
            Assert.Equal("My Cool Static Site", site.Name);
            Assert.Equal(uploadResult.Data.Url, site.Url);
        }

        var userEmail = loginResult.Data.User.Email.ToLowerInvariant().Trim();
        var s3Key = $"sites/{userEmail}/my-cool-static-site";
        var expectedHtmlPath = Path.Combine(_factory.FakeS3.RootDir, s3Key, "index.html");
        var expectedCssPath = Path.Combine(_factory.FakeS3.RootDir, s3Key, "style.css");

        Assert.True(File.Exists(expectedHtmlPath));
        Assert.True(File.Exists(expectedCssPath));
        Assert.Equal("<h1>Hello World</h1>", File.ReadAllText(expectedHtmlPath));
        Assert.Equal("body { color: red; }", File.ReadAllText(expectedCssPath));
    }

    [Fact]
    public async Task UploadSite_WithWrapperFolderInZip_ShouldFlattenAndUploadToS3Root()
    {
        var tempSourceDir = Path.Combine(Path.GetTempPath(), "source_" + Guid.NewGuid());
        var wrapperDir = Path.Combine(tempSourceDir, "my-wrapper-dir");
        Directory.CreateDirectory(wrapperDir);
        File.WriteAllText(Path.Combine(wrapperDir, "index.html"), "<h1>Hello Nested World</h1>");

        var tempZipPath = Path.Combine(Path.GetTempPath(), "site_wrapped_" + Guid.NewGuid() + ".zip");
        ZipFile.CreateFromDirectory(tempSourceDir, tempZipPath);

        Directory.Delete(tempSourceDir, true);

        var client = _factory.CreateClient();

        var registerRequest = new RegisterRequest
        {
            Username = "testsiteuser2",
            Email = "siteuser2@example.com",
            Password = "password123"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginRequest = new LoginRequest
        {
            Email = "siteuser2@example.com",
            Password = "password123"
        };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.NotNull(loginResult?.Data);
        var token = loginResult.Data.Token;

        using var requestContent = new MultipartFormDataContent();
        var fileStream = File.OpenRead(tempZipPath);
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        requestContent.Add(streamContent, "File", "site_wrapped.zip");
        requestContent.Add(new StringContent("Wrapped Site"), "Name");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uploadResponse = await client.PostAsync("/api/sites/upload", requestContent);

        fileStream.Close();
        File.Delete(tempZipPath);

        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<SiteResponse>>();
        Assert.NotNull(uploadResult);
        Assert.False(uploadResult.IsError);
        Assert.NotNull(uploadResult.Data);
        
        var userEmail = loginResult.Data.User.Email.ToLowerInvariant().Trim();
        var s3Key = $"sites/{userEmail}/wrapped-site";
        var expectedHtmlPath = Path.Combine(_factory.FakeS3.RootDir, s3Key, "index.html");

        Assert.True(File.Exists(expectedHtmlPath));
        Assert.Equal("<h1>Hello Nested World</h1>", File.ReadAllText(expectedHtmlPath));
    }

    [Fact]
    public async Task UploadSite_WithMacMetadataInZip_ShouldFilterMetadataAndUploadOnlyWebFilesToS3Root()
    {
        var tempSourceDir = Path.Combine(Path.GetTempPath(), "source_" + Guid.NewGuid());
        var wrapperDir = Path.Combine(tempSourceDir, "site-folder");
        Directory.CreateDirectory(wrapperDir);
        File.WriteAllText(Path.Combine(wrapperDir, "index.html"), "<h1>Hello Clean World</h1>");
        File.WriteAllText(Path.Combine(wrapperDir, ".DS_Store"), "mock ds_store");

        var macOsMetadataDir = Path.Combine(tempSourceDir, "__MACOSX");
        Directory.CreateDirectory(macOsMetadataDir);
        File.WriteAllText(Path.Combine(macOsMetadataDir, "index.html"), "mac metadata");

        var tempZipPath = Path.Combine(Path.GetTempPath(), "site_dirty_" + Guid.NewGuid() + ".zip");
        ZipFile.CreateFromDirectory(tempSourceDir, tempZipPath);

        Directory.Delete(tempSourceDir, true);

        var client = _factory.CreateClient();

        var registerRequest = new RegisterRequest
        {
            Username = "testsiteuser3",
            Email = "siteuser3@example.com",
            Password = "password123"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginRequest = new LoginRequest
        {
            Email = "siteuser3@example.com",
            Password = "password123"
        };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.NotNull(loginResult?.Data);
        var token = loginResult.Data.Token;

        using var requestContent = new MultipartFormDataContent();
        var fileStream = File.OpenRead(tempZipPath);
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        requestContent.Add(streamContent, "File", "site_dirty.zip");
        requestContent.Add(new StringContent("Clean Site"), "Name");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uploadResponse = await client.PostAsync("/api/sites/upload", requestContent);

        fileStream.Close();
        File.Delete(tempZipPath);

        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<SiteResponse>>();
        Assert.NotNull(uploadResult?.Data);


        var s3Key = $"sites/{loginResult.Data.User.Email.ToLowerInvariant().Trim()}/clean-site";
        var expectedHtmlPath = Path.Combine(_factory.FakeS3.RootDir, s3Key, "index.html");
        var expectedDsStorePath = Path.Combine(_factory.FakeS3.RootDir, s3Key, ".DS_Store");
        var expectedMacDir = Path.Combine(_factory.FakeS3.RootDir, s3Key, "__MACOSX");

        Assert.True(File.Exists(expectedHtmlPath));
        Assert.False(File.Exists(expectedDsStorePath));
        Assert.False(Directory.Exists(expectedMacDir));
    }

    [Fact]
    public async Task GetSites_ReturnsOnlyCurrentUserSites()
    {
        var tempSourceDir = Path.Combine(Path.GetTempPath(), "source_" + Guid.NewGuid());
        Directory.CreateDirectory(tempSourceDir);
        File.WriteAllText(Path.Combine(tempSourceDir, "index.html"), "<h1>Test Site</h1>");
        var tempZipPath = Path.Combine(Path.GetTempPath(), "site_" + Guid.NewGuid() + ".zip");
        ZipFile.CreateFromDirectory(tempSourceDir, tempZipPath);
        Directory.Delete(tempSourceDir, true);

        var client = _factory.CreateClient();

        var userAEmail = $"usera_{Guid.NewGuid()}@example.com";
        var registerA = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = "usera",
            Email = userAEmail,
            Password = "password123"
        });
        Assert.Equal(HttpStatusCode.OK, registerA.StatusCode);

        var loginResponseA = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = userAEmail,
            Password = "password123"
        });
        var loginResultA = await loginResponseA.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var tokenA = loginResultA!.Data!.Token;

        var userBEmail = $"userb_{Guid.NewGuid()}@example.com";
        var registerB = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = "userb",
            Email = userBEmail,
            Password = "password123"
        });
        Assert.Equal(HttpStatusCode.OK, registerB.StatusCode);

        var loginResponseB = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = userBEmail,
            Password = "password123"
        });
        var loginResultB = await loginResponseB.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var tokenB = loginResultB!.Data!.Token;

        using (var requestContent = new MultipartFormDataContent())
        {
            using var fileStream = File.OpenRead(tempZipPath);
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            requestContent.Add(streamContent, "File", "site.zip");
            requestContent.Add(new StringContent("User A Site"), "Name");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
            var uploadResponse = await client.PostAsync("/api/sites/upload", requestContent);
            Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        }

        using (var requestContent = new MultipartFormDataContent())
        {
            using var fileStream = File.OpenRead(tempZipPath);
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            requestContent.Add(streamContent, "File", "site.zip");
            requestContent.Add(new StringContent("User B Site"), "Name");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
            var uploadResponse = await client.PostAsync("/api/sites/upload", requestContent);
            Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        }

        File.Delete(tempZipPath);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var getResponseA = await client.GetAsync("/api/sites");
        Assert.Equal(HttpStatusCode.OK, getResponseA.StatusCode);
        var resultA = await getResponseA.Content.ReadFromJsonAsync<ApiResponse<List<SiteResponse>>>();
        Assert.NotNull(resultA?.Data);
        Assert.Single(resultA.Data);
        Assert.Equal("User A Site", resultA.Data[0].Name);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var getResponseB = await client.GetAsync("/api/sites");
        Assert.Equal(HttpStatusCode.OK, getResponseB.StatusCode);
        var resultB = await getResponseB.Content.ReadFromJsonAsync<ApiResponse<List<SiteResponse>>>();
        Assert.NotNull(resultB?.Data);
        Assert.Single(resultB.Data);
        Assert.Equal("User B Site", resultB.Data[0].Name);
    }

    [Fact]
    public async Task GetSites_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync("/api/sites");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
