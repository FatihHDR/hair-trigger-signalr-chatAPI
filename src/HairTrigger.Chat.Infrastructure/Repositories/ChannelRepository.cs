using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using HairTrigger.Chat.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HairTrigger.Chat.Infrastructure.Repositories;

public class ChannelRepository : IChannelRepository
{
    private readonly ChatDbContext _context;

    public ChannelRepository(ChatDbContext context)
    {
        _context = context;
    }

    public async Task<Channel?> GetByIdAsync(Guid id)
    {
        return await _context.Channels.FindAsync(id);
    }

    public async Task<Channel> CreateAsync(Channel channel)
    {
        _context.Channels.Add(channel);
        await _context.SaveChangesAsync();
        return channel;
    }

    public async Task<IEnumerable<Channel>> GetUserChannelsAsync(Guid userId)
    {
        return await _context.ChannelMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.Channel)
            .Where(c => c.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Channel>> GetActiveChannelsAsync()
    {
        return await _context.Channels
            .Where(c => c.IsActive)
            .ToListAsync();
    }

    public async Task AddMemberAsync(Guid channelId, Guid userId, ChannelMemberRole role = ChannelMemberRole.Member)
    {
        var exists = await IsMemberAsync(channelId, userId);
        if (!exists)
        {
            _context.ChannelMembers.Add(new ChannelMember
            {
                ChannelId = channelId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow,
                Role = role
            });
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveMemberAsync(Guid channelId, Guid userId)
    {
        var member = await _context.ChannelMembers
            .FirstOrDefaultAsync(cm => cm.ChannelId == channelId && cm.UserId == userId);
        
        if (member != null)
        {
            _context.ChannelMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsMemberAsync(Guid channelId, Guid userId)
    {
        return await _context.ChannelMembers
            .AnyAsync(cm => cm.ChannelId == channelId && cm.UserId == userId);
    }
}
