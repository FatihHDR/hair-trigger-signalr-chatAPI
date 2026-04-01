using System.Collections.Concurrent;
using HairTrigger.Chat.Domain.Queue;

namespace HairTrigger.Chat.Infrastructure.Queue;

/// <summary>
/// In-memory message queue for development/testing without Redis
/// </summary>
public class InMemoryMessageQueue : IMessageQueue
{
    private readonly ConcurrentQueue<QueueCommand> _queue = new();

    public Task EnqueueAsync<T>(T command, CancellationToken cancellationToken = default) where T : QueueCommand
    {
        _queue.Enqueue(command);
        return Task.CompletedTask;
    }

    public Task<QueueCommand?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        if (_queue.TryDequeue(out var item))
        {
            return Task.FromResult<QueueCommand?>(item);
        }

        return Task.FromResult<QueueCommand?>(null);
    }

    public Task<long> GetQueueLengthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_queue.Count);
    }
}
