namespace HostMe.Domain.Services.Models;

public record LoginResult(string Token, string RefreshToken, UserDto User);
