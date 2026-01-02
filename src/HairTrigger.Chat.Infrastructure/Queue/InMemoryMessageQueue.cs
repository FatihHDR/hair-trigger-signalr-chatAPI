using System.Collections.Concurrent;
using HairTrigger.Chat.Domain.Queue;

namespace HairTrigger.Chat.Infrastructure.Queue;

/// <summary>
/// In-memory message queue for development/testing without Redis
/// </summary>
public class InMemoryMessageQueue : IMessageQueue
{
    private readonly ConcurrentQueue<object> _queue = new();

    public Task EnqueueAsync<T>(T command, CancellationToken cancellationToken = default) where T : class
    {
        _queue.Enqueue(command);
        return Task.CompletedTask;
    }

    public Task<T?> DequeueAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (_queue.TryDequeue(out var item) && item is T typedItem)
        {
            return Task.FromResult<T?>(typedItem);
        }
        return Task.FromResult<T?>(null);
    }

    public Task<long> GetQueueLengthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_queue.Count);
    }
}
