using HostMe.Domain.Services.Models;

namespace HostMe.Host.Models;

public record LoginResponse(string Token, string RefreshToken, UserDto User);
