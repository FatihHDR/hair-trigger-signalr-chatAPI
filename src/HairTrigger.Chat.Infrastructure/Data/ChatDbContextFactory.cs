using HairTrigger.Chat.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HairTrigger.Chat.Infrastructure;

public class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();
        
        // Use environment variable for connection string (more secure)
        // Set environment variable: $env:MIGRATION_CONNECTION_STRING="Host=localhost;Port=5432;Database=hairtrigger_chat;Username=postgres;Password=your_password"
        var connectionString = Environment.GetEnvironmentVariable("MIGRATION_CONNECTION_STRING") 
            ?? "Host=localhost;Port=5432;Database=hairtrigger_chat;Username=postgres;Password=postgres";
        
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null));

        return new ChatDbContext(optionsBuilder.Options);
    }
}
