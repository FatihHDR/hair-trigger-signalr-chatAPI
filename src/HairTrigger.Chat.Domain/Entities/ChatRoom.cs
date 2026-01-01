namespace HairTrigger.Chat.Domain.Entities;

public class ChatRoom
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public List<string> MemberIds { get; set; } = new();
}
