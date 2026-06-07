namespace HostMe.Domain.Security;

using HostMe.Domain.Entities;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
