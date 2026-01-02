namespace HairTrigger.Chat.Domain.Entities;

public class DeliveryStatus
{
    public Guid UserId { get; set; }
    public Guid MessageId { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? SeenAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Message Message { get; set; } = null!;
}
