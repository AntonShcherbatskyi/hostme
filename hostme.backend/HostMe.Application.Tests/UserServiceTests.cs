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
    private readonly UserService _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<HostMeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HostMeDbContext(options);
        var userRepository = new HostMe.Persistance.Repositories.UserRepository(_dbContext);
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _userService = new UserService(userRepository, _passwordHasher);
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
}
