using System.Text;

namespace HostMe.Application;

public static class SlugHelper
{
    public static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var c in text.ToLowerInvariant().Trim())
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (c is ' ' or '-' or '_')
                sb.Append('-');
        }

        var result = sb.ToString().Trim('-');

        while (result.Contains("--"))
            result = result.Replace("--", "-");

        return result;
    }
}
