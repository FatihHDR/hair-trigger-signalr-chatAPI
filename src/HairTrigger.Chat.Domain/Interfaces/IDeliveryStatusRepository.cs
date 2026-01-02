using HairTrigger.Chat.Domain.Entities;

namespace HairTrigger.Chat.Domain.Interfaces;

public interface IDeliveryStatusRepository
{
    Task<DeliveryStatus> CreateOrUpdateAsync(DeliveryStatus status);
    Task MarkDeliveredAsync(Guid userId, Guid messageId);
    Task MarkSeenAsync(Guid userId, Guid messageId);
    Task MarkSeenUpToOffsetAsync(Guid userId, Guid channelId, long offset);
    Task<IEnumerable<DeliveryStatus>> GetForMessageAsync(Guid messageId);
    Task<long?> GetLastSeenOffsetAsync(Guid userId, Guid channelId);
}
