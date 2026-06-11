using HostMe.Domain.Services;

namespace HostMe.Infrastructure.Storage;

public class FileSystemS3Service : IS3Service
{
    public string RootDir { get; }

    public FileSystemS3Service()
    {
        RootDir = Path.Combine(Path.GetTempPath(), "hostme_fake_s3_" + Guid.NewGuid());
        Directory.CreateDirectory(RootDir);
    }

    public Task UploadFolderAsync(string localPath, string s3Prefix, CancellationToken cancellationToken = default)
    {
        var targetDir = Path.Combine(RootDir, s3Prefix);
        Directory.CreateDirectory(targetDir);

        var files = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(localPath, file);
            var destPath = Path.Combine(targetDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(file, destPath, overwrite: true);
        }

        return Task.CompletedTask;
    }

    public Task DeleteFolderAsync(string s3Prefix, CancellationToken cancellationToken = default)
    {
        var targetDir = Path.Combine(RootDir, s3Prefix);
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, recursive: true);
        }
        return Task.CompletedTask;
    }

    public string GetSiteUrl(string s3Key)
    {
        return $"file://{Path.Combine(RootDir, s3Key).Replace("\\", "/")}/index.html";
    }
}
