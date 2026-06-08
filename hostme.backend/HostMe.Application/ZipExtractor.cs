using System.IO.Compression;

namespace HostMe.Application;

public static class ZipExtractor
{
    public static void Extract(Stream zipStream, string destinationDir)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;

            var fullPath = Path.GetFullPath(Path.Combine(destinationDir, entry.FullName));

            if (!fullPath.StartsWith(destinationDir, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"ZipSlip detected: {entry.FullName}");

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            entry.ExtractToFile(fullPath, overwrite: true);
        }
    }

    public static void RemoveMacOsMetadata(string dir)
    {
        foreach (var macDir in Directory.GetDirectories(dir, "__MACOSX", SearchOption.AllDirectories))
            try { Directory.Delete(macDir, recursive: true); } catch { }

        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase) || name.StartsWith("._"))
                try { File.Delete(file); } catch { }
        }
    }

    public static string ResolveUploadRoot(string baseDir)
    {
        var entries = Directory.GetFileSystemEntries(baseDir);
        return entries.Length == 1 && Directory.Exists(entries[0]) ? entries[0] : baseDir;
    }
}
