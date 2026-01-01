using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using HairTrigger.Chat.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HairTrigger.Chat.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly ChatDbContext _context;

    public MessageRepository(ChatDbContext context)
    {
        _context = context;
    }

    public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(string? roomId = null, int take = 50)
    {
        var query = _context.Messages.Where(m => !m.IsDeleted);

        if (!string.IsNullOrEmpty(roomId))
        {
            query = query.Where(m => m.RoomId == roomId);
        }

        return await query
            .OrderByDescending(m => m.SentAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<ChatMessage?> GetMessageByIdAsync(Guid id)
    {
        return await _context.Messages.FindAsync(id);
    }

    public async Task DeleteMessageAsync(Guid id)
    {
        var message = await GetMessageByIdAsync(id);
        if (message != null)
        {
            message.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }
}
