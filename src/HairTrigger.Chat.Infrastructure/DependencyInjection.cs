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
        var connectionString = configuration.GetConnectionString("ChatDatabase");
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        var nameTranslator = new NpgsqlSnakeCaseNameTranslator();
        dataSourceBuilder.MapEnum<ChatRoomType>("public.chat_rooms_room_type_enum", nameTranslator);
        dataSourceBuilder.MapEnum<MessageType>("public.chat_messages_message_type_enum", nameTranslator);
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);

        // Add DbContext with PostgreSQL — connects to existing chat_isj database
        services.AddDbContext<ChatDbContext>(options =>
            options.UseNpgsql(
                dataSource,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null)));

        // Add Redis
        var redisConnectionString = configuration.GetConnectionString("Redis");
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
}
