using HostMe.Domain.Entities;
using HostMe.Domain.Security;
using HostMe.Domain.Services.Models;
using HostMe.Domain.Constants;
using HostMe.Persistance;
using HostMe.Application;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HostMe.Tests;

public class UserServiceTests : IDisposable
{
    private readonly HostMeDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<HostMeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HostMeDbContext(options);
        var userRepository = new HostMe.Persistance.Repositories.UserRepository(_dbContext);
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _userService = new UserService(userRepository, _passwordHasher, _jwtTokenGenerator);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ShouldCreateUserAndReturnDto()
    {
        var request = new RegisterRequest
        {
            Username = "johndoe",
            Email = "johndoe@example.com",
            Password = "password123"
        };
        _passwordHasher.HashPassword(request.Password).Returns("hashed_password");

        var result = await _userService.RegisterAsync(request);

        Assert.NotNull(result);
        Assert.Equal(request.Username, result.Username);
        Assert.Equal(request.Email, result.Email);
        Assert.NotEqual(Guid.Empty, result.Id);

        var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("johndoe", savedUser.Username);
        Assert.Equal("johndoe@example.com", savedUser.Email);
        Assert.Equal("hashed_password", savedUser.PasswordHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task RegisterAsync_WithInvalidUsername_ShouldThrowArgumentException(string? username)
    {
        var request = new RegisterRequest
        {
            Username = username!,
            Email = "test@example.com",
            Password = "password123"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.RegisterAsync(request));
        Assert.Contains(ErrorMessages.User.UsernameRequired, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task RegisterAsync_WithInvalidEmail_ShouldThrowArgumentException(string? email)
    {
        var request = new RegisterRequest
        {
            Username = "johndoe",
            Email = email!,
            Password = "password123"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.RegisterAsync(request));
        Assert.Contains(ErrorMessages.User.EmailRequired, exception.Message);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    public async Task RegisterAsync_WithMalformedEmail_ShouldThrowArgumentException(string email)
    {
        var request = new RegisterRequest
        {
            Username = "johndoe",
            Email = email,
            Password = "password123"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.RegisterAsync(request));
        Assert.Contains(ErrorMessages.User.EmailInvalid, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData(null)]
    public async Task RegisterAsync_WithInvalidPassword_ShouldThrowArgumentException(string? password)
    {
        var request = new RegisterRequest
        {
            Username = "johndoe",
            Email = "test@example.com",
            Password = password!
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.RegisterAsync(request));
        Assert.Contains(ErrorMessages.User.PasswordLength, exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "existing",
            Email = "duplicate@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "duplicate@example.com",
            Password = "password123"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.RegisterAsync(request));
        Assert.Contains(ErrorMessages.User.EmailTaken, exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ShouldThrowInvalidOperationException()
    {
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "duplicate",
            Email = "existing@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "duplicate",
            Email = "new@example.com",
            Password = "password123"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.RegisterAsync(request));
        Assert.Contains(ErrorMessages.User.UsernameTaken, exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithValidEmail_ShouldReturnTokenAndDto()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "johndoe",
            Email = "johndoe@example.com",
            PasswordHash = "hashed_password",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "johndoe@example.com",
            Password = "password123"
        };

        _passwordHasher.VerifyPassword(request.Password, user.PasswordHash).Returns(true);
        _jwtTokenGenerator.GenerateToken(Arg.Is<User>(u => u.Id == user.Id)).Returns("mocked_token");

        var result = await _userService.LoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal("mocked_token", result.Token);
        Assert.NotEmpty(result.RefreshToken);
        Assert.Equal(user.Username, result.User.Username);
        Assert.Equal(user.Email, result.User.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task LoginAsync_WithMissingEmail_ShouldThrowArgumentException(string? email)
    {
        var request = new LoginRequest
        {
            Email = email!,
            Password = "password123"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.LoginAsync(request));
        Assert.Contains(ErrorMessages.User.EmailRequired, exception.Message);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    public async Task LoginAsync_WithMalformedEmail_ShouldThrowArgumentException(string email)
    {
        var request = new LoginRequest
        {
            Email = email,
            Password = "password123"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.LoginAsync(request));
        Assert.Contains(ErrorMessages.User.EmailInvalid, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task LoginAsync_WithMissingPassword_ShouldThrowArgumentException(string? password)
    {
        var request = new LoginRequest
        {
            Email = "johndoe@example.com",
            Password = password!
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.LoginAsync(request));
        Assert.Contains(ErrorMessages.User.PasswordRequired, exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ShouldThrowArgumentException()
    {
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.LoginAsync(request));
        Assert.Contains(ErrorMessages.User.InvalidCredentials, exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ShouldThrowArgumentException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "johndoe",
            Email = "johndoe@example.com",
            PasswordHash = "hashed_password",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "johndoe@example.com",
            Password = "wrongpassword"
        };

        _passwordHasher.VerifyPassword(request.Password, user.PasswordHash).Returns(false);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.LoginAsync(request));
        Assert.Contains(ErrorMessages.User.InvalidCredentials, exception.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidActiveToken_ShouldRotateTokens()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "johndoe",
            Email = "johndoe@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        var oldRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "old_token",
            ExpiryUtc = DateTime.UtcNow.AddDays(1),
            CreatedUtc = DateTime.UtcNow,
            UserId = user.Id
        };
        user.RefreshTokens.Add(oldRefreshToken);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "old_token" };
        _jwtTokenGenerator.GenerateToken(Arg.Is<User>(u => u.Id == user.Id)).Returns("new_access_token");

        var result = await _userService.RefreshTokenAsync(request);

        Assert.NotNull(result);
        Assert.Equal("new_access_token", result.Token);
        Assert.NotEqual("old_token", result.RefreshToken);
        Assert.NotEmpty(result.RefreshToken);

        var refreshedUser = await _dbContext.Users.Include(u => u.RefreshTokens).FirstAsync(u => u.Id == user.Id);
        var oldToken = refreshedUser.RefreshTokens.First(t => t.Token == "old_token");
        Assert.NotNull(oldToken.RevokedUtc);

        var newToken = refreshedUser.RefreshTokens.First(t => t.Token == result.RefreshToken);
        Assert.Null(newToken.RevokedUtc);
        Assert.False(newToken.IsExpired);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldThrowArgumentException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "johndoe",
            Email = "johndoe@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "expired_token",
            ExpiryUtc = DateTime.UtcNow.AddDays(-1),
            CreatedUtc = DateTime.UtcNow.AddDays(-2),
            UserId = user.Id
        };
        user.RefreshTokens.Add(expiredToken);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "expired_token" };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.RefreshTokenAsync(request));
        Assert.Contains(ErrorMessages.User.InvalidRefreshToken, exception.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ShouldThrowArgumentException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "johndoe",
            Email = "johndoe@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "revoked_token",
            ExpiryUtc = DateTime.UtcNow.AddDays(1),
            CreatedUtc = DateTime.UtcNow,
            RevokedUtc = DateTime.UtcNow,
            UserId = user.Id
        };
        user.RefreshTokens.Add(revokedToken);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "revoked_token" };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _userService.RefreshTokenAsync(request));
        Assert.Contains(ErrorMessages.User.InvalidRefreshToken, exception.Message);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_ShouldRevokeToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "johndoe",
            Email = "johndoe@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        var activeToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "active_token",
            ExpiryUtc = DateTime.UtcNow.AddDays(1),
            CreatedUtc = DateTime.UtcNow,
            UserId = user.Id
        };
        user.RefreshTokens.Add(activeToken);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new RevokeTokenRequest { RefreshToken = "active_token" };

        await _userService.RevokeTokenAsync(request);

        var refreshedUser = await _dbContext.Users.Include(u => u.RefreshTokens).FirstAsync(u => u.Id == user.Id);
        var token = refreshedUser.RefreshTokens.First(t => t.Token == "active_token");
        Assert.NotNull(token.RevokedUtc);
        Assert.True(token.IsRevoked);
    }
}
