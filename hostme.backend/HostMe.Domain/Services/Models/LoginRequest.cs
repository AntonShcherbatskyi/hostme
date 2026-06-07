using System.ComponentModel.DataAnnotations;
using HostMe.Domain.Constants;

namespace HostMe.Domain.Services.Models;

public record LoginRequest
{
    [Required(ErrorMessage = ErrorMessages.User.EmailRequired)]
    [EmailAddress(ErrorMessage = ErrorMessages.User.EmailInvalid)]
    public string Email { get; init; } = null!;

    [Required(ErrorMessage = ErrorMessages.User.PasswordRequired)]
    public string Password { get; init; } = null!;
}
