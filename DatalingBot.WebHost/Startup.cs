using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class Startup
{
    public static async Task InitializeAsync(IServiceProvider services, ICustomLogger logger, bool isDevelopment)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogWarning("Pending migrations found:");
            foreach (var m in pendingMigrations)
                logger.LogWarning($" - {m}");

            if (isDevelopment)
            {
                logger.LogInformation("Recreating database (Development only)...");
                await db.Database.EnsureDeletedAsync();
            }
        }

        logger.LogInformation("Applying migrations...");
        await db.Database.MigrateAsync();

        logger.LogInformation("Database ready!");
    }
}
