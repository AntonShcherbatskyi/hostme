using System.ComponentModel.DataAnnotations;

namespace HostMe.Infrastructure.Options;

public class S3Options
{
    public const string SectionName = "S3";

    [Required(ErrorMessage = "S3 BucketName is required.")]
    public string BucketName { get; set; } = string.Empty;

    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? ServiceUrl { get; set; }
    public string? Region { get; set; }
}
