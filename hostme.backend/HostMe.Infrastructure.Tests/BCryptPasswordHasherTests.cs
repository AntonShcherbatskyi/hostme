using HostMe.Infrastructure.Security;
using Xunit;

namespace HostMe.Tests;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher;

    public BCryptPasswordHasherTests()
    {
        _hasher = new BCryptPasswordHasher();
    }

    [Fact]
    public void HashPassword_ShouldReturnHashedString()
    {
        var password = "SuperSecretPassword123";

        var hash = _hasher.HashPassword(password);

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var password = "SuperSecretPassword123";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword(password, hash);

        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        var password = "SuperSecretPassword123";
        var wrongPassword = "WrongPassword123";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword(wrongPassword, hash);

        Assert.False(isValid);
    }
}
