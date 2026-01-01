using HairTrigger.Chat.Domain.Entities;

namespace HairTrigger.Chat.Domain.Interfaces;

public interface IMessageRepository
{
    Task<ChatMessage> AddMessageAsync(ChatMessage message);
    Task<IEnumerable<ChatMessage>> GetMessagesAsync(string? roomId = null, int take = 50);
    Task<ChatMessage?> GetMessageByIdAsync(Guid id);
    Task DeleteMessageAsync(Guid id);
}
