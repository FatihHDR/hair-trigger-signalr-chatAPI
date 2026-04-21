namespace HairTrigger.Chat.Domain.Queue;

public interface IMessageQueue
{
    Task EnqueueAsync<T>(T command, CancellationToken cancellationToken = default) where T : QueueCommand;
    Task<QueueCommand?> DequeueAsync(CancellationToken cancellationToken = default);
    Task<long> GetQueueLengthAsync(CancellationToken cancellationToken = default);
}

// Command types for the queue
public abstract record QueueCommand(DateTime EnqueuedAt);

public record SendMessageCommand(
    Guid RoomId,
    Guid SenderReferenceId,
    string Content,
    string ConnectionId,
    DateTime EnqueuedAt
) : QueueCommand(EnqueuedAt);

public record UserConnectedCommand(
    Guid UserReferenceId,
    string ConnectionId,
    DateTime EnqueuedAt
) : QueueCommand(EnqueuedAt);

public record UserDisconnectedCommand(
    Guid UserReferenceId,
    string ConnectionId,
    DateTime EnqueuedAt
) : QueueCommand(EnqueuedAt);
