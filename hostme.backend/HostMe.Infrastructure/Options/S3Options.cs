using System.ComponentModel.DataAnnotations;
using HostMe.Domain.Constants;

namespace HostMe.Infrastructure.Options;

public class S3Options
{
    public const string SectionName = "S3";

    [Required(ErrorMessage = ErrorMessages.Validation.S3BucketNameRequired)]
    public string BucketName { get; set; } = string.Empty;

    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? ServiceUrl { get; set; }
    public string? Region { get; set; }
}
