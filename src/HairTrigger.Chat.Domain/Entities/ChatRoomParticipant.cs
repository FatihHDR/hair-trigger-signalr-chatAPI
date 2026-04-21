namespace HairTrigger.Chat.Domain.Entities;

public class ChatRoomParticipant
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid UserReferenceId { get; set; } // Reference to primary DB user (cross-DB)
    public string Role { get; set; } = string.Empty; // e.g. "client", "counselor"
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ChatRoom Room { get; set; } = null!;
}
