using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using HairTrigger.Chat.Infrastructure.Data;
using HairTrigger.Chat.Infrastructure.Repositories;
using HairTrigger.Chat.Infrastructure.Queue;
using HairTrigger.Chat.Domain.Queue;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.NameTranslation;
using StackExchange.Redis;

namespace HairTrigger.Chat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rawConnectionString = configuration.GetConnectionString("ChatDatabase") 
            ?? configuration["CHAT_DB_CONNECTION_STRING"]
            ?? Environment.GetEnvironmentVariable("CHAT_DB_CONNECTION_STRING");

        var connectionString = NormalizeConnectionString(rawConnectionString);

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        var nameTranslator = new NpgsqlSnakeCaseNameTranslator();
        dataSourceBuilder.MapEnum<ChatRoomType>("chat_rooms_room_type_enum", nameTranslator);
        dataSourceBuilder.MapEnum<MessageType>("chat_messages_message_type_enum", nameTranslator);
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);

        // Add DbContext with PostgreSQL — connects to existing chat_isj database
        services.AddDbContext<ChatDbContext>(options =>
            options.UseNpgsql(
                dataSource,
                npgsqlOptions =>
                {
                    npgsqlOptions.MapEnum<ChatRoomType>("chat_rooms_room_type_enum", nameTranslator: nameTranslator);
                    npgsqlOptions.MapEnum<MessageType>("chat_messages_message_type_enum", nameTranslator: nameTranslator);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                }));

        // Add Redis
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["REDIS_CONNECTION_STRING"]
            ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var config = ConfigurationOptions.Parse(redisConnectionString);
                config.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(config);
            });
            
            // Register Redis message queue
            services.AddSingleton<IMessageQueue, RedisMessageQueue>();
        }
        else
        {
            // Use in-memory queue for development without Redis
            services.AddSingleton<IMessageQueue, InMemoryMessageQueue>();
        }

        // Register repositories
        services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();

        return services;
    }

    private static string NormalizeConnectionString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "Host=localhost;Port=5432;Database=chat_isj;Username=postgres;Password=12345678";
        }

        raw = raw.Trim();

        // Handle postgres:// or postgresql:// URIs
        if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(raw);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "postgres";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var database = uri.AbsolutePath.TrimStart('/');

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = database,
                Username = username,
                Password = password
            };
            return builder.ConnectionString;
        }

        // Try standard parsing
        try
        {
            var test = new NpgsqlConnectionStringBuilder(raw);
            return test.ConnectionString;
        }
        catch (ArgumentException)
        {
            // Fix unquoted password containing semicolons
            var pwdIdx = raw.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
            if (pwdIdx >= 0)
            {
                var prefix = raw.Substring(0, pwdIdx + 9);
                var remainder = raw.Substring(pwdIdx + 9);

                if (!remainder.StartsWith("'") && !remainder.StartsWith("\""))
                {
                    // Find next keyword like ;Host= or ;Database= or end of string
                    var nextParamIdx = System.Text.RegularExpressions.Regex.Match(
                        remainder, 
                        @";(Host|Port|Database|Username|User Id|SSL Mode|Trust Server Certificate)=", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    string pwdValue;
                    string suffix = "";
                    if (nextParamIdx.Success)
                    {
                        pwdValue = remainder.Substring(0, nextParamIdx.Index);
                        suffix = remainder.Substring(nextParamIdx.Index);
                    }
                    else
                    {
                        pwdValue = remainder;
                    }

                    var escapedPwd = "'" + pwdValue.Replace("'", "''") + "'";
                    return prefix + escapedPwd + suffix;
                }
            }
            throw;
        }
    }
}
