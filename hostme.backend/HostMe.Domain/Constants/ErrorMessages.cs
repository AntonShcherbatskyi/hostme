namespace HostMe.Domain.Constants;

public static class ErrorMessages
{
    public static class User
    {
        public const string UsernameRequired = "Username is required.";
        public const string UsernameLength = "Username must be between 3 and 100 characters.";
        public const string UsernameTaken = "Username is already taken.";

        public const string EmailRequired = "Email is required.";
        public const string EmailInvalid = "Invalid email address.";
        public const string EmailLength = "Email cannot be longer than 256 characters.";
        public const string EmailTaken = "Email is already registered.";

        public const string PasswordRequired = "Password is required.";
        public const string PasswordLength = "Password must be at least 6 characters long.";
        public const string UsernameOrEmailRequired = "Username or email is required.";
        public const string InvalidCredentials = "Invalid username/email or password.";
        public const string RefreshTokenRequired = "Refresh token is required.";
        public const string InvalidRefreshToken = "Invalid or expired refresh token.";
        public const string Unathorized = "User is unathorized";
    }

    public static class General
    {
        public const string UnexpectedError = "An unexpected error occurred.";
        public const string UnexpectedErrorWithDetail = "An unexpected error occurred: ";
        public const string DatabaseConnectionFailed = "Database connection failed.";
    }

    public static class Site
    {
        public const string ContentTypeZIPSupported = "Only ZIP files are supported";
        public const string NotFound = "Site not found.";
        public const string Forbidden = "You do not have permission to delete this site.";
        public const string UserNotFound = "User not found.";
        public const string MissingIndexHtml = "The ZIP must contain an index.html file at the root.";
        public const string DisallowedFileType =
            "The ZIP contains a file with a disallowed extension: {0}. Only HTML, CSS, JS, JSON, and image files are allowed.";
        public const string ZipSlipDetected = "ZipSlip detected: {0}";
    }
    
    public static class Validation
    {
        public const string FileRequired = "ZIP file is required.";
        public const string SiteNameRequired = "Site name is required.";
        public const string SiteNameLength = "Site name cannot exceed 200 characters.";

        public const string JwtSecretRequired = "JWT Secret is required.";
        public const string JwtSecretMinLength = "JWT Secret must be at least 32 characters long.";
        public const string JwtIssuerRequired = "JWT Issuer is required.";
        public const string JwtAudienceRequired = "JWT Audience is required.";
        public const string JwtExpiryRange = "ExpiryMinutes must be greater than 0.";

        public const string DatabaseConnectionRequired =
            "Database connection string 'DefaultConnection' is required.";
        public const string S3BucketNameRequired = "S3 BucketName is required.";
    }
}
