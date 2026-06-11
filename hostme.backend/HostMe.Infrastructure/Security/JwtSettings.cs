using System.ComponentModel.DataAnnotations;
using HostMe.Domain.Constants;

namespace HostMe.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = ErrorMessages.Validation.JwtSecretRequired)]
    [MinLength(32, ErrorMessage = ErrorMessages.Validation.JwtSecretMinLength)]
    public string Secret { get; set; } = null!;

    [Required(ErrorMessage = ErrorMessages.Validation.JwtIssuerRequired)]
    public string Issuer { get; set; } = null!;

    [Required(ErrorMessage = ErrorMessages.Validation.JwtAudienceRequired)]
    public string Audience { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = ErrorMessages.Validation.JwtExpiryRange)]
    public int ExpiryMinutes { get; set; }
}
