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
    }

    public static class General
    {
        public const string UnexpectedError = "An unexpected error occurred: ";
        public const string DatabaseConnectionFailed = "Database connection failed.";
    }
}
