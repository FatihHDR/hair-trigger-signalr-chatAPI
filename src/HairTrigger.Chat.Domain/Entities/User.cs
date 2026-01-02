namespace HairTrigger.Chat.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<DeliveryStatus> DeliveryStatuses { get; set; } = new List<DeliveryStatus>();
    public ICollection<ChannelMember> ChannelMemberships { get; set; } = new List<ChannelMember>();
}
