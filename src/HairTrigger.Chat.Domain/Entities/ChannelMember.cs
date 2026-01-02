namespace HairTrigger.Chat.Domain.Entities;

public class ChannelMember
{
    public Guid ChannelId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }
    public ChannelMemberRole Role { get; set; } = ChannelMemberRole.Member;
    
    // Navigation properties
    public Channel Channel { get; set; } = null!;
    public User User { get; set; } = null!;
}

public enum ChannelMemberRole
{
    Member,
    Admin,
    Owner
}
