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

    public async Task<Message> AddMessageAsync(Message message)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<IEnumerable<Message>> GetChannelMessagesAsync(Guid channelId, long? fromOffset = null, int take = 50)
    {
        var query = _context.Messages
            .Where(m => m.ChannelId == channelId && !m.IsDeleted);

        if (fromOffset.HasValue)
        {
            query = query.Where(m => m.Offset > fromOffset.Value);
        }

        return await query
            .OrderBy(m => m.Offset)
            .Take(take)
            .Include(m => m.Sender)
            .ToListAsync();
    }

    public async Task<Message?> GetByIdAsync(Guid id)
    {
        return await _context.Messages.FindAsync(id);
    }

    public async Task<long> GetNextOffsetAsync(Guid channelId)
    {
        var maxOffset = await _context.Messages
            .Where(m => m.ChannelId == channelId)
            .MaxAsync(m => (long?)m.Offset);
        
        return (maxOffset ?? 0) + 1;
    }

    public async Task DeleteMessageAsync(Guid id)
    {
        var message = await GetByIdAsync(id);
        if (message != null)
        {
            message.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }
}
