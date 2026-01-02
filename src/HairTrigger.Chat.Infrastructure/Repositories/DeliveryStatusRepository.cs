using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using HairTrigger.Chat.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HairTrigger.Chat.Infrastructure.Repositories;

public class DeliveryStatusRepository : IDeliveryStatusRepository
{
    private readonly ChatDbContext _context;

    public DeliveryStatusRepository(ChatDbContext context)
    {
        _context = context;
    }

    public async Task<DeliveryStatus> CreateOrUpdateAsync(DeliveryStatus status)
    {
        var existing = await _context.DeliveryStatuses
            .FirstOrDefaultAsync(ds => ds.UserId == status.UserId && ds.MessageId == status.MessageId);
        
        if (existing != null)
        {
            existing.DeliveredAt = status.DeliveredAt ?? existing.DeliveredAt;
            existing.SeenAt = status.SeenAt ?? existing.SeenAt;
        }
        else
        {
            _context.DeliveryStatuses.Add(status);
        }
        
        await _context.SaveChangesAsync();
        return existing ?? status;
    }

    public async Task MarkDeliveredAsync(Guid userId, Guid messageId)
    {
        var status = await _context.DeliveryStatuses
            .FirstOrDefaultAsync(ds => ds.UserId == userId && ds.MessageId == messageId);
        
        if (status == null)
        {
            _context.DeliveryStatuses.Add(new DeliveryStatus
            {
                UserId = userId,
                MessageId = messageId,
                DeliveredAt = DateTime.UtcNow
            });
        }
        else if (status.DeliveredAt == null)
        {
            status.DeliveredAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task MarkSeenAsync(Guid userId, Guid messageId)
    {
        var status = await _context.DeliveryStatuses
            .FirstOrDefaultAsync(ds => ds.UserId == userId && ds.MessageId == messageId);
        
        if (status == null)
        {
            _context.DeliveryStatuses.Add(new DeliveryStatus
            {
                UserId = userId,
                MessageId = messageId,
                DeliveredAt = DateTime.UtcNow,
                SeenAt = DateTime.UtcNow
            });
        }
        else if (status.SeenAt == null)
        {
            status.DeliveredAt ??= DateTime.UtcNow;
            status.SeenAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task MarkSeenUpToOffsetAsync(Guid userId, Guid channelId, long offset)
    {
        var messages = await _context.Messages
            .Where(m => m.ChannelId == channelId && m.Offset <= offset)
            .Select(m => m.Id)
            .ToListAsync();
        
        foreach (var messageId in messages)
        {
            await MarkSeenAsync(userId, messageId);
        }
    }

    public async Task<IEnumerable<DeliveryStatus>> GetForMessageAsync(Guid messageId)
    {
        return await _context.DeliveryStatuses
            .Where(ds => ds.MessageId == messageId)
            .Include(ds => ds.User)
            .ToListAsync();
    }

    public async Task<long?> GetLastSeenOffsetAsync(Guid userId, Guid channelId)
    {
        return await _context.DeliveryStatuses
            .Where(ds => ds.UserId == userId && ds.Message.ChannelId == channelId && ds.SeenAt != null)
            .Select(ds => (long?)ds.Message.Offset)
            .MaxAsync();
    }
}
