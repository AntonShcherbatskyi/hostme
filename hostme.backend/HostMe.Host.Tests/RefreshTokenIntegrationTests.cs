using System.Net;
using System.Net.Http.Json;
using HostMe.Domain.Entities;
using HostMe.Domain.Security;
using HostMe.Domain.Services.Models;
using HostMe.Host.Models;
using HostMe.Persistance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HostMe.Host.Tests;

public class RefreshTokenIntegrationTests : IClassFixture<HostMeWebApplicationFactory>
{
    private readonly HostMeWebApplicationFactory _factory;

    public RefreshTokenIntegrationTests(HostMeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LoginAndRefresh_WithValidFlow_ShouldRotateTokensAndThenRevokeSuccessfully()
    {
        var client = _factory.CreateClient();
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HostMeDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "integrationuser",
                Email = "integration@example.com",
                PasswordHash = hasher.HashPassword("password123"),
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var loginRequest = new LoginRequest
        {
            Email = "integration@example.com",
            Password = "password123"
        };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.NotNull(loginResult);
        Assert.False(loginResult.IsError);
        Assert.NotNull(loginResult.Data);
        Assert.NotEmpty(loginResult.Data.Token);
        Assert.NotEmpty(loginResult.Data.RefreshToken);

        var originalRefreshToken = loginResult.Data.RefreshToken;

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = originalRefreshToken
        };
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.NotNull(refreshResult);
        Assert.False(refreshResult.IsError);
        Assert.NotNull(refreshResult.Data);
        Assert.NotEmpty(refreshResult.Data.Token);
        Assert.NotEmpty(refreshResult.Data.RefreshToken);
        Assert.NotEqual(originalRefreshToken, refreshResult.Data.RefreshToken);

        var newRefreshToken = refreshResult.Data.RefreshToken;

        var staleRefreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        Assert.Equal(HttpStatusCode.BadRequest, staleRefreshResponse.StatusCode);

        var staleResult = await staleRefreshResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(staleResult);
        Assert.True(staleResult.IsError);
        Assert.NotEmpty(staleResult.Errors);

        var revokeRequest = new RevokeTokenRequest
        {
            RefreshToken = newRefreshToken
        };
        var revokeResponse = await client.PostAsJsonAsync("/api/auth/revoke", revokeRequest);
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        var postRevokeRefreshRequest = new RefreshTokenRequest
        {
            RefreshToken = newRefreshToken
        };
        var postRevokeRefreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", postRevokeRefreshRequest);
        Assert.Equal(HttpStatusCode.BadRequest, postRevokeRefreshResponse.StatusCode);
    }
}
