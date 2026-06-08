using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HostMe.Host.Models;

public class UploadSiteRequest
{
    [Required(ErrorMessage = "ZIP file is required.")]
    public IFormFile File { get; set; } = null!;

    [Required(ErrorMessage = "Site name is required.")]
    [StringLength(200, ErrorMessage = "Site name cannot exceed 200 characters.")]
    public string Name { get; set; } = null!;
}
