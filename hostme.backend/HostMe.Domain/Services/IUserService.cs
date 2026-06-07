using HostMe.Domain.Services.Models;

namespace HostMe.Domain.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
