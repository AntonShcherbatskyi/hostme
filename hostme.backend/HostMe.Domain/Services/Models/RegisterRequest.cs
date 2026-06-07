using System.ComponentModel.DataAnnotations;
using HostMe.Domain.Constants;

namespace HostMe.Domain.Services.Models;

public record RegisterRequest
{
    [Required(ErrorMessage = ErrorMessages.User.UsernameRequired)]
    [StringLength(100, MinimumLength = 3, ErrorMessage = ErrorMessages.User.UsernameLength)]
    public string Username { get; init; } = null!;

    [Required(ErrorMessage = ErrorMessages.User.EmailRequired)]
    [EmailAddress(ErrorMessage = ErrorMessages.User.EmailInvalid)]
    [StringLength(256, ErrorMessage = ErrorMessages.User.EmailLength)]
    public string Email { get; init; } = null!;

    [Required(ErrorMessage = ErrorMessages.User.PasswordRequired)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = ErrorMessages.User.PasswordLength)]
    public string Password { get; init; } = null!;
}
