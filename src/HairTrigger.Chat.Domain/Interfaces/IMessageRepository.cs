using HairTrigger.Chat.Domain.Entities;

namespace HairTrigger.Chat.Domain.Interfaces;

public interface IMessageRepository
{
    Task<Message> AddMessageAsync(Message message);
    Task<IEnumerable<Message>> GetChannelMessagesAsync(Guid channelId, long? fromOffset = null, int take = 50);
    Task<Message?> GetByIdAsync(Guid id);
    Task<long> GetNextOffsetAsync(Guid channelId);
    Task DeleteMessageAsync(Guid id);
}
