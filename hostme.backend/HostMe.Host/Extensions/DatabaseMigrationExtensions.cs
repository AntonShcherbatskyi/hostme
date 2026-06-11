using HostMe.Domain.Constants;
using HostMe.Persistance;
using Microsoft.EntityFrameworkCore;

namespace HostMe.Host.Extensions;

public static class DatabaseMigrationExtensions
{
    private const int MaxRetries = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        if (app.Environment.IsEnvironment(AppEnvironments.Testing))
            return app;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HostMeDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(LogMessages.Database.CategoryName);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                db.Database.Migrate();
                logger.LogInformation(LogMessages.Database.MigrationsApplied);
                return app;
            }
            catch (Exception ex)
            {
                if (attempt == MaxRetries)
                    throw;

                logger.LogWarning(
                    ex,
                    LogMessages.Database.MigrationAttemptFailed,
                    attempt,
                    MaxRetries,
                    RetryDelay.TotalSeconds);

                Thread.Sleep(RetryDelay);
            }
        }

        return app;
    }
}
