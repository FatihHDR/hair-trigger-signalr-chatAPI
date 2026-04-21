using HairTrigger.Chat.Domain.Entities;

namespace HairTrigger.Chat.Domain.Interfaces;

public interface IChatMessageRepository
{
    Task<ChatMessage> AddMessageAsync(ChatMessage message);
    Task<IEnumerable<ChatMessage>> GetRoomMessagesAsync(Guid roomId, int take = 50, DateTime? before = null);
    Task<ChatMessage?> GetByIdAsync(Guid id);
    Task DeleteMessageAsync(Guid id);
}
