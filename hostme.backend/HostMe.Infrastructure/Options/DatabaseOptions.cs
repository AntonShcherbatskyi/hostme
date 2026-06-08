using System.ComponentModel.DataAnnotations;

namespace HostMe.Infrastructure.Options;

public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required(ErrorMessage = "Database connection string 'DefaultConnection' is required.")]
    public string DefaultConnection { get; set; } = string.Empty;
}
