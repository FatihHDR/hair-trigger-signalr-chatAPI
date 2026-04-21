namespace HairTrigger.Chat.Domain.Entities;

public class ChatTranscript
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid GeneratedBy { get; set; } // Reference to primary DB user (cross-DB)
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ChatRoom Room { get; set; } = null!;
}
