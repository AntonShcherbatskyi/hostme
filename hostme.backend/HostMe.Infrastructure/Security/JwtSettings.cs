using System.ComponentModel.DataAnnotations;

namespace HostMe.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "JWT Secret is required.")]
    [MinLength(32, ErrorMessage = "JWT Secret must be at least 32 characters long.")]
    public string Secret { get; set; } = null!;

    [Required(ErrorMessage = "JWT Issuer is required.")]
    public string Issuer { get; set; } = null!;

    [Required(ErrorMessage = "JWT Audience is required.")]
    public string Audience { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "ExpiryMinutes must be greater than 0.")]
    public int ExpiryMinutes { get; set; }
}
