using HairTrigger.Chat.Domain.Entities;

namespace HairTrigger.Chat.Domain.Interfaces;

public interface IRoomRepository
{
    Task<ChatRoom> CreateRoomAsync(ChatRoom room);
    Task<ChatRoom?> GetRoomByIdAsync(string id);
    Task<IEnumerable<ChatRoom>> GetActiveRoomsAsync();
    Task AddMemberToRoomAsync(string roomId, string userId);
    Task RemoveMemberFromRoomAsync(string roomId, string userId);
}
