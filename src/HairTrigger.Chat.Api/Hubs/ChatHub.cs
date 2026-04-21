using HairTrigger.Chat.Domain.Queue;
using HairTrigger.Chat.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace HairTrigger.Chat.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageQueue _messageQueue;
    private readonly IChatRoomRepository _chatRoomRepository;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IMessageQueue messageQueue,
        IChatRoomRepository chatRoomRepository,
        ILogger<ChatHub> logger)
    {
        _messageQueue = messageQueue;
        _chatRoomRepository = chatRoomRepository;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserReferenceId();
        _logger.LogInformation("User {UserReferenceId} connected with ConnectionId {ConnectionId}", userId, Context.ConnectionId);

        // Join user to their personal group for direct notifications
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        // Get user's active rooms and join those groups
        var rooms = await _chatRoomRepository.GetUserRoomsAsync(userId);
        foreach (var room in rooms)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{room.Id}");
            _logger.LogDebug("User {UserReferenceId} joined room group {RoomId}", userId, room.Id);
        }

        // Enqueue connected event for worker
        await _messageQueue.EnqueueAsync(new UserConnectedCommand(
            UserReferenceId: userId,
            ConnectionId: Context.ConnectionId,
            EnqueuedAt: DateTime.UtcNow
        ));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserReferenceId();
        _logger.LogInformation("User {UserReferenceId} disconnected. Exception: {Exception}", userId, exception?.Message);

        // Enqueue disconnected event for worker
        await _messageQueue.EnqueueAsync(new UserDisconnectedCommand(
            UserReferenceId: userId,
            ConnectionId: Context.ConnectionId,
            EnqueuedAt: DateTime.UtcNow
        ));

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Sends a message to a chat room. Validates membership and enqueues for worker processing.
    /// </summary>
    public async Task SendMessageToRoom(Guid roomId, string content)
    {
        var userId = GetUserReferenceId();
        
        // Validate room participation
        var isParticipant = await _chatRoomRepository.IsParticipantAsync(roomId, userId);
        if (!isParticipant)
        {
            _logger.LogWarning("User {UserReferenceId} attempted to send message to room {RoomId} without participation", userId, roomId);
            throw new HubException("You are not a participant of this room");
        }

        // Validate content
        if (string.IsNullOrWhiteSpace(content) || content.Length > 4000)
        {
            throw new HubException("Message content is invalid or too long");
        }

        // Enqueue message command for worker to process
        await _messageQueue.EnqueueAsync(new SendMessageCommand(
            RoomId: roomId,
            SenderReferenceId: userId,
            Content: content.Trim(),
            ConnectionId: Context.ConnectionId,
            EnqueuedAt: DateTime.UtcNow
        ));

        _logger.LogDebug("Message queued from user {UserReferenceId} to room {RoomId}", userId, roomId);
    }

    /// <summary>
    /// Joins a room group (called after being added as a participant)
    /// </summary>
    public async Task JoinRoom(Guid roomId)
    {
        var userId = GetUserReferenceId();

        var isParticipant = await _chatRoomRepository.IsParticipantAsync(roomId, userId);
        if (!isParticipant)
        {
            throw new HubException("You are not a participant of this room");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId}");
        _logger.LogInformation("User {UserReferenceId} joined room {RoomId}", userId, roomId);
    }

    /// <summary>
    /// Leaves a room group
    /// </summary>
    public async Task LeaveRoom(Guid roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{roomId}");
        _logger.LogInformation("User {UserReferenceId} left room {RoomId}", GetUserReferenceId(), roomId);
    }

    private Guid GetUserReferenceId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? Context.User?.FindFirst("sub")?.Value;
        
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        throw new HubException("Unauthorized: user identifier claim is missing or invalid");
    }
}
