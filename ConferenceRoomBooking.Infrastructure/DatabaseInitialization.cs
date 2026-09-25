using ConferenceRoomBooking.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace ConferenceRoomBooking.Infrastructure;

public static class DatabaseInitialization
{
    public static async Task SeedDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
