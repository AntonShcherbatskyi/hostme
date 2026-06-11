namespace HostMe.Domain.Constants;

public static class StorageConstants
{
    public const string SiteKeyPrefix = "sites";
    public const string IndexHtmlFile = "index.html";
    public const string DefaultAwsRegion = "us-east-1";

    public const string TempDirPrefix = "hostme_";
    public const string FakeS3DirPrefix = "hostme_fake_s3_";

    /// <summary>Format: bucket, region, s3Key.</summary>
    public const string AwsS3UrlTemplate = "https://{0}.s3.{1}.amazonaws.com/{2}/index.html";

    public const string LocalDirectoryNotFound = "Local directory not found: {0}";

    public const string AllFilesGlob = "*";
}
