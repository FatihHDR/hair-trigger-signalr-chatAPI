namespace HairTrigger.Chat.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid SenderReferenceId { get; set; } // Reference to primary DB user (cross-DB)
    public MessageType MessageType { get; set; } = MessageType.Text;
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ChatRoom Room { get; set; } = null!;
}
