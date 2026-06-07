using System.ComponentModel.DataAnnotations;
using HostMe.Domain.Constants;

namespace HostMe.Domain.Services.Models;

public record RevokeTokenRequest
{
    [Required(ErrorMessage = ErrorMessages.User.RefreshTokenRequired)]
    public string RefreshToken { get; init; } = null!;
}
