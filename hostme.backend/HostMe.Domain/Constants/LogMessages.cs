namespace HostMe.Domain.Constants;

public static class LogMessages
{
    public static class Database
    {
        public const string CategoryName = "DatabaseMigration";
        public const string MigrationsApplied = "Database migrations applied successfully.";
        public const string MigrationAttemptFailed =
            "Database migration attempt {Attempt}/{MaxRetries} failed, retrying in {DelaySeconds}s...";
    }

    public static class Http
    {
        public const string UnhandledException = "An unhandled exception occurred.";
    }
}
