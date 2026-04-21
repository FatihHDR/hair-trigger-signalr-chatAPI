using System.Text.Json;
using HairTrigger.Chat.Domain.Queue;
using StackExchange.Redis;

namespace HairTrigger.Chat.Infrastructure.Queue;

public class RedisMessageQueue : IMessageQueue
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _queueKey = "chat:message-queue";

    public RedisMessageQueue(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task EnqueueAsync<T>(T command, CancellationToken cancellationToken = default) where T : QueueCommand
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(command, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        var wrapper = new QueueMessageWrapper
        {
            TypeName = typeof(T).Name,
            Payload = json
        };
        
        await db.ListRightPushAsync(_queueKey, JsonSerializer.Serialize(wrapper));
    }

    public async Task<QueueCommand?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.ListLeftPopAsync(_queueKey);
        
        if (value.IsNullOrEmpty)
            return null;
        
        var valueString = value.ToString();
        var wrapper = JsonSerializer.Deserialize<QueueMessageWrapper>(valueString);
        if (wrapper == null)
            return null;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return wrapper.TypeName switch
        {
            nameof(SendMessageCommand) => JsonSerializer.Deserialize<SendMessageCommand>(wrapper.Payload, options),
            nameof(UserConnectedCommand) => JsonSerializer.Deserialize<UserConnectedCommand>(wrapper.Payload, options),
            nameof(UserDisconnectedCommand) => JsonSerializer.Deserialize<UserDisconnectedCommand>(wrapper.Payload, options),
            _ => null
        };
    }

    public async Task<long> GetQueueLengthAsync(CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        return await db.ListLengthAsync(_queueKey);
    }
}

internal class QueueMessageWrapper
{
    public string TypeName { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}
