using ConferenceRoomBooking.Infrastructure.Persistence;
using ConferenceRoomBooking.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConferenceRoomBooking.Infrastructure;

public static class DatabaseInitialization
{
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        bool applyMigrations,
        bool seedInitialData,
        CancellationToken cancellationToken = default)
    {
        if (!applyMigrations && !seedInitialData)
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        if (applyMigrations)
        {
            logger.LogInformation("Applying pending database migrations.");
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations completed.");
        }

        logger.LogInformation("Seeding initial database records.");
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(cancellationToken);
        logger.LogInformation("Database seeding completed.");
    }
}
