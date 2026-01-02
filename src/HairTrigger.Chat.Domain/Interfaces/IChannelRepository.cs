using HairTrigger.Chat.Domain.Entities;

namespace HairTrigger.Chat.Domain.Interfaces;

public interface IChannelRepository
{
    Task<Channel?> GetByIdAsync(Guid id);
    Task<Channel> CreateAsync(Channel channel);
    Task<IEnumerable<Channel>> GetUserChannelsAsync(Guid userId);
    Task<IEnumerable<Channel>> GetActiveChannelsAsync();
    Task AddMemberAsync(Guid channelId, Guid userId, ChannelMemberRole role = ChannelMemberRole.Member);
    Task RemoveMemberAsync(Guid channelId, Guid userId);
    Task<bool> IsMemberAsync(Guid channelId, Guid userId);
}
