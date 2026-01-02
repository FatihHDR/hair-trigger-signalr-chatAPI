namespace HairTrigger.Chat.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public long Offset { get; set; } // Per-channel incremental offset
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public Channel Channel { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public ICollection<DeliveryStatus> DeliveryStatuses { get; set; } = new List<DeliveryStatus>();
}
