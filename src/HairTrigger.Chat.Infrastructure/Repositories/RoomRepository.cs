using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using HairTrigger.Chat.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HairTrigger.Chat.Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly ChatDbContext _context;

    public RoomRepository(ChatDbContext context)
    {
        _context = context;
    }

    public async Task<ChatRoom> CreateRoomAsync(ChatRoom room)
    {
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        return room;
    }

    public async Task<ChatRoom?> GetRoomByIdAsync(string id)
    {
        return await _context.Rooms.FindAsync(id);
    }

    public async Task<IEnumerable<ChatRoom>> GetActiveRoomsAsync()
    {
        return await _context.Rooms
            .Where(r => r.IsActive)
            .ToListAsync();
    }

    public async Task AddMemberToRoomAsync(string roomId, string userId)
    {
        var room = await GetRoomByIdAsync(roomId);
        if (room != null && !room.MemberIds.Contains(userId))
        {
            room.MemberIds.Add(userId);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveMemberFromRoomAsync(string roomId, string userId)
    {
        var room = await GetRoomByIdAsync(roomId);
        if (room != null)
        {
            room.MemberIds.Remove(userId);
            await _context.SaveChangesAsync();
        }
    }
}
