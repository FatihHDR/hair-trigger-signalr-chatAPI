using HairTrigger.Chat.Domain.Entities;

namespace HairTrigger.Chat.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User> CreateAsync(User user);
    Task<IEnumerable<User>> GetUsersInChannelAsync(Guid channelId);
}
