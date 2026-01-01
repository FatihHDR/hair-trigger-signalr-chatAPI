using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace HairTrigger.Chat.Api.Hubs;

public class ChatHub : Hub
{
    private readonly IMessageRepository _messageRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IMessageRepository messageRepository,
        IRoomRepository roomRepository,
        ILogger<ChatHub> logger)
    {
        _messageRepository = messageRepository;
        _roomRepository = roomRepository;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string userId, string userName, string content, string? roomId = null)
    {
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            Content = content,
            RoomId = roomId,
            SentAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _messageRepository.AddMessageAsync(message);

        if (!string.IsNullOrEmpty(roomId))
        {
            await Clients.Group(roomId).SendAsync("ReceiveMessage", message);
        }
        else
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }

        _logger.LogInformation("Message sent by {UserName} in room {RoomId}", userName, roomId ?? "global");
    }

    public async Task JoinRoom(string roomId, string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await _roomRepository.AddMemberToRoomAsync(roomId, userId);
        
        await Clients.Group(roomId).SendAsync("UserJoined", userId, roomId);
        _logger.LogInformation("User {UserId} joined room {RoomId}", userId, roomId);
    }

    public async Task LeaveRoom(string roomId, string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        await _roomRepository.RemoveMemberFromRoomAsync(roomId, userId);
        
        await Clients.Group(roomId).SendAsync("UserLeft", userId, roomId);
        _logger.LogInformation("User {UserId} left room {RoomId}", userId, roomId);
    }

    public async Task<IEnumerable<ChatMessage>> GetRecentMessages(string? roomId = null, int count = 50)
    {
        return await _messageRepository.GetMessagesAsync(roomId, count);
    }
}
