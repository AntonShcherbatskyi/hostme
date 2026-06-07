using HostMe.Domain.Services;
using HostMe.Domain.Services.Models;
using HostMe.Host.Models;
using Microsoft.AspNetCore.Mvc;

namespace HostMe.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.RegisterAsync(request, cancellationToken);
        var response = new RegisterResponse(result.Id, result.Username, result.Email, result.CreatedAt);
        return Ok(ApiResponse<RegisterResponse>.Success(response));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.LoginAsync(request, cancellationToken);
        var response = new LoginResponse(result.Token, result.RefreshToken, result.User);
        return Ok(ApiResponse<LoginResponse>.Success(response));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.RefreshTokenAsync(request, cancellationToken);
        var response = new LoginResponse(result.Token, result.RefreshToken, result.User);
        return Ok(ApiResponse<LoginResponse>.Success(response));
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        await _userService.RevokeTokenAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Success(null!));
    }
}
