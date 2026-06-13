using System.IO.Compression;
using HostMe.Application.Constants;
using HostMe.Domain.Constants;

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
                throw new InvalidOperationException(
                    string.Format(ErrorMessages.Site.ZipSlipDetected, entry.FullName));

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            entry.ExtractToFile(fullPath, overwrite: true);
        }
    }

    public static void RemoveMacOsMetadata(string dir)
    {
        foreach (var macDir in Directory.GetDirectories(
            dir, ZipConstants.MacOsMetadataDir, SearchOption.AllDirectories))
        {
            try { Directory.Delete(macDir, recursive: true); } catch { }
        }

        foreach (var file in Directory.GetFiles(
            dir, StorageConstants.AllFilesGlob, SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (name.Equals(ZipConstants.DsStoreFile, StringComparison.OrdinalIgnoreCase)
                || name.StartsWith(ZipConstants.MacOsResourceForkPrefix))
            {
                try { File.Delete(file); } catch { }
            }
        }
    }

    public static string ResolveUploadRoot(string baseDir)
    {
        var entries = Directory.GetFileSystemEntries(baseDir);
        return entries.Length == 1 && Directory.Exists(entries[0]) ? entries[0] : baseDir;
    }
}
