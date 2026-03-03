using Microsoft.EntityFrameworkCore;

namespace AutoPC.Data;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending migrations and initializes the database.
    /// Should be called during application startup.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        try
        {
            // Apply any pending migrations
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
            Console.WriteLine("WARNING: Server will continue without database. Chat history sync will be unavailable.");
        }
    }

    /// <summary>
    /// Synchronous version for use in Program.cs if async is not available.
    /// </summary>
    public static void InitializeDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        try
        {
            // Apply any pending migrations
            dbContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
            Console.WriteLine("WARNING: Server will continue without database. Chat history sync will be unavailable.");
        }
    }
}
