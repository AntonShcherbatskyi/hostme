using System.Diagnostics;
using HostMe.Persistance;
using Microsoft.EntityFrameworkCore;

namespace HostMe.Host.Extensions;

public static class DatabaseMigrationExtensions
{
    private const int MaxRetries = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    public static WebApplication ApplyMigrations(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        if (app.Environment.IsEnvironment("Testing"))
            return app;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HostMeDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigration");

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                db.Database.Migrate();
                logger.LogInformation("Database migrations applied successfully.");
                return app;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                logger.LogWarning(
                    ex,
                    "Migration attempt {Attempt}/{MaxRetries} failed, retrying in {DelaySeconds}s...",
                    attempt,
                    MaxRetries,
                    RetryDelay.TotalSeconds);

                Thread.Sleep(RetryDelay);
            }
        }

        throw new UnreachableException();
    }
}
