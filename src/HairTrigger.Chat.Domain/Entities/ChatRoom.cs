namespace HairTrigger.Chat.Domain.Entities;

public class ChatRoom
{
    public Guid Id { get; set; }
    public ChatRoomType RoomType { get; set; }
    public Guid? SessionReferenceId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<ChatRoomParticipant> Participants { get; set; } = new List<ChatRoomParticipant>();
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public ICollection<ChatTranscript> Transcripts { get; set; } = new List<ChatTranscript>();
}
