namespace HostMe.Domain.Services;

public interface IS3Service
{
    Task UploadFolderAsync(string localPath, string s3Prefix, CancellationToken cancellationToken = default);
    Task DeleteFolderAsync(string s3Prefix, CancellationToken cancellationToken = default);
    string GetSiteUrl(string s3Key);
}
