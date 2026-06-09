using HostMe.Domain.Constants;

namespace HostMe.Application;

public static class SiteFileValidator
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".html", ".htm",
        ".css",
        ".js", ".mjs",
        ".json",
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".webp", ".avif",
        ".txt", ".xml",
        ".woff", ".woff2", ".ttf", ".eot",
        ".map",
    };
    
    public static void Validate(string uploadDir)
    {
        var indexHtml = Path.Combine(uploadDir, "index.html");
        if (!File.Exists(indexHtml))
            throw new ArgumentException(ErrorMessages.Site.MissingIndexHtml);

        foreach (var file in Directory.EnumerateFiles(uploadDir, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            if (!AllowedExtensions.Contains(ext))
            {
                var relativePath = Path.GetRelativePath(uploadDir, file).Replace("\\", "/");
                throw new ArgumentException(
                    string.Format(ErrorMessages.Site.DisallowedFileType, relativePath));
            }
        }
    }
}
