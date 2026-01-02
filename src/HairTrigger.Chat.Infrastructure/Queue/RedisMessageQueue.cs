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

    public async Task EnqueueAsync<T>(T command, CancellationToken cancellationToken = default) where T : class
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(command, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        // Use type name as wrapper for deserialization
        var wrapper = new QueueMessageWrapper
        {
            TypeName = typeof(T).Name,
            Payload = json
        };
        
        await db.ListRightPushAsync(_queueKey, JsonSerializer.Serialize(wrapper));
    }

    public async Task<T?> DequeueAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        var db = _redis.GetDatabase();
        var value = await db.ListLeftPopAsync(_queueKey);
        
        if (value.IsNullOrEmpty)
            return null;
        
        var wrapper = JsonSerializer.Deserialize<QueueMessageWrapper>(value!);
        if (wrapper == null)
            return null;
        
        return JsonSerializer.Deserialize<T>(wrapper.Payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
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
