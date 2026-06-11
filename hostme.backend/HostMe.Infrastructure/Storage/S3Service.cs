using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using HostMe.Domain.Services;
using HostMe.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace HostMe.Infrastructure.Storage;

public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _options;

    public S3Service(IOptions<S3Options> options)
    {
        _options = options.Value;

        var config = new AmazonS3Config();
        if (!string.IsNullOrEmpty(_options.ServiceUrl))
        {
            config.ServiceURL = _options.ServiceUrl;
            config.ForcePathStyle = true;
        }
        else if (!string.IsNullOrEmpty(_options.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region);
        }

        if (!string.IsNullOrEmpty(_options.AccessKey) && !string.IsNullOrEmpty(_options.SecretKey))
        {
            var credentials = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
            _s3Client = new AmazonS3Client(credentials, config);
        }
        else
        {
            _s3Client = new AmazonS3Client(config);
        }
    }

    public async Task UploadFolderAsync(string localPath, string s3Prefix, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(localPath))
        {
            throw new DirectoryNotFoundException($"Local directory not found: {localPath}");
        }

        var files = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(localPath, file).Replace("\\", "/");
            var s3Key = string.IsNullOrEmpty(s3Prefix) ? relativePath : $"{s3Prefix.TrimEnd('/')}/{relativePath}";

            var putRequest = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = s3Key,
                FilePath = file,
                ContentType = GetContentType(file)
            };

            await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        }
    }

    public async Task DeleteFolderAsync(string s3Prefix, CancellationToken cancellationToken = default)
    {
        var listRequest = new ListObjectsV2Request
        {
            BucketName = _options.BucketName,
            Prefix = s3Prefix
        };

        ListObjectsV2Response listResponse;
        do
        {
            listResponse = await _s3Client.ListObjectsV2Async(listRequest, cancellationToken);

            if (listResponse.S3Objects.Count > 0)
            {
                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = _options.BucketName,
                    Objects = listResponse.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList()
                };

                await _s3Client.DeleteObjectsAsync(deleteRequest, cancellationToken);
            }

            listRequest.ContinuationToken = listResponse.NextContinuationToken;
        } while (listResponse.IsTruncated == true);
    }

    public string GetSiteUrl(string s3Key)
    {
        if (!string.IsNullOrEmpty(_options.ServiceUrl))
        {
            return $"{_options.ServiceUrl.TrimEnd('/')}/{_options.BucketName}/{s3Key}/index.html";
        }

        var region = _options.Region ?? "us-east-1";
        return $"https://{_options.BucketName}.s3.{region}.amazonaws.com/{s3Key}/index.html";
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".txt" => "text/plain",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".eot" => "application/vnd.ms-fontobject",
            _ => "application/octet-stream"
        };
    }
}
