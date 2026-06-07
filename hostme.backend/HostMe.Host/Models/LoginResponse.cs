using HostMe.Domain.Services.Models;

namespace HostMe.Host.Models;

public record LoginResponse(string Token, UserDto User);
