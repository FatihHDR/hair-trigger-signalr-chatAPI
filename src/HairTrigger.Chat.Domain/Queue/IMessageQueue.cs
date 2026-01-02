namespace HairTrigger.Chat.Domain.Queue;

public interface IMessageQueue
{
    Task EnqueueAsync<T>(T command, CancellationToken cancellationToken = default) where T : class;
    Task<T?> DequeueAsync<T>(CancellationToken cancellationToken = default) where T : class;
    Task<long> GetQueueLengthAsync(CancellationToken cancellationToken = default);
}

// Command types for the queue
public abstract record QueueCommand(DateTime EnqueuedAt);

public record SendMessageCommand(
    Guid ChannelId,
    Guid SenderId,
    string Content,
    string ConnectionId,
    DateTime EnqueuedAt
) : QueueCommand(EnqueuedAt);

public record MarkSeenCommand(
    Guid ChannelId,
    Guid UserId,
    long LastSeenOffset,
    DateTime EnqueuedAt
) : QueueCommand(EnqueuedAt);

public record UserConnectedCommand(
    Guid UserId,
    string ConnectionId,
    DateTime EnqueuedAt
) : QueueCommand(EnqueuedAt);

public record UserDisconnectedCommand(
    Guid UserId,
    string ConnectionId,
    DateTime EnqueuedAt
) : QueueCommand(EnqueuedAt);
