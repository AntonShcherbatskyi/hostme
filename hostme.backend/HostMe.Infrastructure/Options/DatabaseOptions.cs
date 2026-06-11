using System.ComponentModel.DataAnnotations;
using HostMe.Domain.Constants;

namespace HostMe.Infrastructure.Options;

public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required(ErrorMessage = ErrorMessages.Validation.DatabaseConnectionRequired)]
    public string DefaultConnection { get; set; } = string.Empty;
}
