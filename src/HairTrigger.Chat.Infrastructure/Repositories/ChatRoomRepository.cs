using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using HairTrigger.Chat.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HairTrigger.Chat.Infrastructure.Repositories;

public class ChatRoomRepository : IChatRoomRepository
{
    private readonly ChatDbContext _context;

    public ChatRoomRepository(ChatDbContext context)
    {
        _context = context;
    }

    public async Task<ChatRoom?> GetByIdAsync(Guid id)
    {
        return await _context.ChatRooms.FindAsync(id);
    }

    public async Task<ChatRoom> CreateAsync(ChatRoom room)
    {
        _context.ChatRooms.Add(room);
        await _context.SaveChangesAsync();
        return room;
    }

    public async Task<IEnumerable<ChatRoom>> GetUserRoomsAsync(Guid userReferenceId)
    {
        return await _context.ChatRoomParticipants
            .Where(p => p.UserReferenceId == userReferenceId && p.LeftAt == null)
            .Select(p => p.Room)
            .Where(r => r.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatRoom>> GetActiveRoomsAsync()
    {
        return await _context.ChatRooms
            .Where(r => r.IsActive)
            .ToListAsync();
    }

    public async Task<ChatRoomParticipant> AddParticipantAsync(Guid roomId, Guid userReferenceId, string role)
    {
        var existing = await _context.ChatRoomParticipants
            .FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserReferenceId == userReferenceId && p.LeftAt == null);

        if (existing != null)
            return existing;

        var participant = new ChatRoomParticipant
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            UserReferenceId = userReferenceId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatRoomParticipants.Add(participant);
        await _context.SaveChangesAsync();
        return participant;
    }

    public async Task RemoveParticipantAsync(Guid roomId, Guid userReferenceId)
    {
        var participant = await _context.ChatRoomParticipants
            .FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserReferenceId == userReferenceId && p.LeftAt == null);

        if (participant != null)
        {
            participant.LeftAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsParticipantAsync(Guid roomId, Guid userReferenceId)
    {
        return await _context.ChatRoomParticipants
            .AnyAsync(p => p.RoomId == roomId && p.UserReferenceId == userReferenceId && p.LeftAt == null);
    }

    public async Task<IEnumerable<ChatRoomParticipant>> GetParticipantsAsync(Guid roomId)
    {
        return await _context.ChatRoomParticipants
            .Where(p => p.RoomId == roomId && p.LeftAt == null)
            .ToListAsync();
    }

    public async Task CloseRoomAsync(Guid roomId)
    {
        var room = await _context.ChatRooms.FindAsync(roomId);
        if (room != null)
        {
            room.IsActive = false;
            room.ClosedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
