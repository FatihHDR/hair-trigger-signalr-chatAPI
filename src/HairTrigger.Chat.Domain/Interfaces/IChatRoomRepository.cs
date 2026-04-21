using HairTrigger.Chat.Domain.Entities;

namespace HairTrigger.Chat.Domain.Interfaces;

public interface IChatRoomRepository
{
    Task<ChatRoom?> GetByIdAsync(Guid id);
    Task<ChatRoom> CreateAsync(ChatRoom room);
    Task<IEnumerable<ChatRoom>> GetUserRoomsAsync(Guid userReferenceId);
    Task<IEnumerable<ChatRoom>> GetActiveRoomsAsync();
    Task<ChatRoomParticipant> AddParticipantAsync(Guid roomId, Guid userReferenceId, string role);
    Task RemoveParticipantAsync(Guid roomId, Guid userReferenceId);
    Task<bool> IsParticipantAsync(Guid roomId, Guid userReferenceId);
    Task<IEnumerable<ChatRoomParticipant>> GetParticipantsAsync(Guid roomId);
    Task CloseRoomAsync(Guid roomId);
}
