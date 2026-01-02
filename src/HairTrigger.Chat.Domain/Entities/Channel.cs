namespace HairTrigger.Chat.Domain.Entities;

public class Channel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ChannelMember> Members { get; set; } = new List<ChannelMember>();
}
