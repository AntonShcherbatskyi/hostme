using HostMe.Host.Controllers;
using HostMe.Domain.Constants;
using HostMe.Domain.Services;
using HostMe.Domain.Services.Models;
using HostMe.Host.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace HostMe.Tests;

public class AuthControllerTests
{
    private readonly IUserService _userService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _userService = Substitute.For<IUserService>();
        _controller = new AuthController(_userService);
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsOkWithResult()
    {
        var request = new RegisterRequest
        {
            Username = "johndoe",
            Email = "johndoe@example.com",
            Password = "password123"
        };
        var expectedDto = new UserDto(Guid.NewGuid(), request.Username, request.Email, DateTime.UtcNow);
        _userService.RegisterAsync(request, Arg.Any<CancellationToken>()).Returns(expectedDto);

        var response = await _controller.Register(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<RegisterResponse>>(okResult.Value);
        Assert.False(apiResponse.IsError);
        Assert.Empty(apiResponse.Errors);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(expectedDto.Id, apiResponse.Data.Id);
        Assert.Equal(expectedDto.Username, apiResponse.Data.Username);
        Assert.Equal(expectedDto.Email, apiResponse.Data.Email);
    }

    [Fact]
    public async Task Register_WithArgumentException_ReturnsBadRequest()
    {
        var request = new RegisterRequest();
        _userService.RegisterAsync(request, Arg.Any<CancellationToken>())
            .Throws(new ArgumentException(ErrorMessages.User.UsernameRequired));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _controller.Register(request, CancellationToken.None));
        Assert.Equal(ErrorMessages.User.UsernameRequired, exception.Message);
    }

    [Fact]
    public async Task Register_WithInvalidOperationException_ReturnsConflict()
    {
        var request = new RegisterRequest();
        _userService.RegisterAsync(request, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException(ErrorMessages.User.EmailTaken));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Register(request, CancellationToken.None));
        Assert.Equal(ErrorMessages.User.EmailTaken, exception.Message);
    }

    [Fact]
    public async Task Register_WithUnexpectedException_ReturnsInternalServerError()
    {
        var request = new RegisterRequest();
        _userService.RegisterAsync(request, Arg.Any<CancellationToken>())
            .Throws(new Exception(ErrorMessages.General.DatabaseConnectionFailed));

        var exception = await Assert.ThrowsAsync<Exception>(() => _controller.Register(request, CancellationToken.None));
        Assert.Equal(ErrorMessages.General.DatabaseConnectionFailed, exception.Message);
    }

    [Fact]
    public async Task Login_WithValidRequest_ReturnsOkWithResult()
    {
        var request = new LoginRequest
        {
            Email = "johndoe@example.com",
            Password = "password123"
        };
        var expectedResult = new LoginResult("mocked_token", new UserDto(Guid.NewGuid(), "johndoe", "johndoe@example.com", DateTime.UtcNow));
        _userService.LoginAsync(request, Arg.Any<CancellationToken>()).Returns(expectedResult);

        var response = await _controller.Login(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<LoginResponse>>(okResult.Value);
        Assert.False(apiResponse.IsError);
        Assert.Empty(apiResponse.Errors);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(expectedResult.Token, apiResponse.Data.Token);
        Assert.Equal(expectedResult.User.Id, apiResponse.Data.User.Id);
        Assert.Equal(expectedResult.User.Username, apiResponse.Data.User.Username);
        Assert.Equal(expectedResult.User.Email, apiResponse.Data.User.Email);
    }

    [Fact]
    public async Task Login_WithArgumentException_ReturnsBadRequest()
    {
        var request = new LoginRequest();
        _userService.LoginAsync(request, Arg.Any<CancellationToken>())
            .Throws(new ArgumentException(ErrorMessages.User.InvalidCredentials));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _controller.Login(request, CancellationToken.None));
        Assert.Equal(ErrorMessages.User.InvalidCredentials, exception.Message);
    }
}
