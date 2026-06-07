using System.IdentityModel.Tokens.Jwt;
using HostMe.Domain.Entities;
using HostMe.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace HostMe.Tests;

public class JwtTokenGeneratorTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly IOptions<JwtSettings> _options;
    private readonly JwtTokenGenerator _tokenGenerator;

    public JwtTokenGeneratorTests()
    {
        _jwtSettings = new JwtSettings
        {
            Secret = "super_secret_key_that_is_at_least_32_characters_long",
            Issuer = "HostMe",
            Audience = "HostMe",
            ExpiryMinutes = 60
        };

        _options = Options.Create(_jwtSettings);
        _tokenGenerator = new JwtTokenGenerator(_options);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidJwtToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "johndoe",
            Email = "johndoe@example.com",
            PasswordHash = "hash"
        };

        var tokenString = _tokenGenerator.GenerateToken(user);

        Assert.NotNull(tokenString);
        Assert.NotEmpty(tokenString);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(tokenString);

        Assert.NotNull(jwtToken);
        Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
        Assert.Contains(jwtToken.Audiences, aud => aud == _jwtSettings.Audience);
        
        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        var uniqueNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName)?.Value;
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;

        Assert.Equal(user.Id.ToString(), subClaim);
        Assert.Equal(user.Username, uniqueNameClaim);
        Assert.Equal(user.Email, emailClaim);
    }
}
