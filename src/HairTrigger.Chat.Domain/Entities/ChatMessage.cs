namespace HairTrigger.Chat.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? RoomId { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsDeleted { get; set; }
}
