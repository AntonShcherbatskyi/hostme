using System.ComponentModel.DataAnnotations;
using HostMe.Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace HostMe.Host.Models;

public class UploadSiteRequest
{
    [Required(ErrorMessage = ErrorMessages.Validation.FileRequired)]
    public IFormFile File { get; set; } = null!;

    [Required(ErrorMessage = ErrorMessages.Validation.SiteNameRequired)]
    [StringLength(200, ErrorMessage = ErrorMessages.Validation.SiteNameLength)]
    public string Name { get; set; } = null!;
}
