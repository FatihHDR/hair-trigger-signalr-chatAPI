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
        var roomList = rooms.ToList();

        foreach (var room in roomList)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{room.Id}");
            _logger.LogDebug("User {UserReferenceId} auto-joined room group {RoomId} on connect", userId, room.Id);
        }

        _logger.LogInformation(
            "User {UserReferenceId} connected. Auto-joined {RoomCount} room(s): [{RoomIds}]",
            userId, roomList.Count, string.Join(", ", roomList.Select(r => r.Id)));

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
    /// Joins a room's SignalR group to receive real-time messages.
    /// Called after connecting, or after being added as a participant mid-session.
    /// </summary>
    public async Task JoinRoom(Guid roomId)
    {
        var userId = GetUserReferenceId();

        var isParticipant = await _chatRoomRepository.IsParticipantAsync(roomId, userId);
        if (!isParticipant)
        {
            _logger.LogWarning(
                "JoinRoom DENIED — User {UserId} is not a participant of room {RoomId}. " +
                "ConnectionId: {ConnectionId}",
                userId, roomId, Context.ConnectionId);

            throw new HubException(
                $"User '{userId}' is not registered as a participant of room '{roomId}'. " +
                $"Ask staff to add you via POST /api/v1/rooms/{roomId}/participants.");
        }

        var room = await _chatRoomRepository.GetByIdAsync(roomId);
        if (room is { IsActive: false })
        {
            _logger.LogWarning(
                "JoinRoom DENIED — Room {RoomId} is already closed. User {UserId}",
                roomId, userId);

            throw new HubException($"Room '{roomId}' is closed and no longer accepting connections.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId}");
        _logger.LogInformation(
            "User {UserId} successfully joined room {RoomId} (ConnectionId: {ConnectionId})",
            userId, roomId, Context.ConnectionId);
    }

    /// <summary>
    /// Sends a message to a chat room. Validates membership and enqueues for processing.
    /// </summary>
    public async Task SendMessageToRoom(Guid roomId, string content)
    {
        var userId = GetUserReferenceId();

        // Validate content first (cheap check)
        if (string.IsNullOrWhiteSpace(content))
            throw new HubException("Message content cannot be empty.");

        if (content.Length > 4000)
            throw new HubException($"Message content is too long ({content.Length} chars). Maximum is 4000 characters.");

        // Validate room participation
        var isParticipant = await _chatRoomRepository.IsParticipantAsync(roomId, userId);
        if (!isParticipant)
        {
            _logger.LogWarning(
                "SendMessageToRoom DENIED — User {UserId} is not a participant of room {RoomId}. " +
                "ConnectionId: {ConnectionId}",
                userId, roomId, Context.ConnectionId);

            throw new HubException(
                $"User '{userId}' is not registered as a participant of room '{roomId}'. " +
                $"Ask staff to add you via POST /api/v1/rooms/{roomId}/participants.");
        }

        // Enqueue message command for worker to process
        await _messageQueue.EnqueueAsync(new SendMessageCommand(
            RoomId: roomId,
            SenderReferenceId: userId,
            Content: content.Trim(),
            ConnectionId: Context.ConnectionId,
            EnqueuedAt: DateTime.UtcNow
        ));

        _logger.LogDebug("Message queued from user {UserId} to room {RoomId}", userId, roomId);
    }

    /// <summary>
    /// Returns debug info about the current user's participation state.
    /// Useful for diagnosing JoinRoom / SendMessageToRoom failures.
    /// </summary>
    public async Task<object> GetMyRoomInfo(Guid roomId)
    {
        var userId = GetUserReferenceId();
        var isParticipant = await _chatRoomRepository.IsParticipantAsync(roomId, userId);
        var room = await _chatRoomRepository.GetByIdAsync(roomId);
        var allMyRooms = await _chatRoomRepository.GetUserRoomsAsync(userId);

        return new
        {
            UserId         = userId,
            RoomId         = roomId,
            IsParticipant  = isParticipant,
            RoomExists     = room != null,
            RoomIsActive   = room?.IsActive,
            MyActiveRooms  = allMyRooms.Select(r => new { r.Id, r.RoomType, r.IsActive })
        };
    }

    /// <summary>
    /// Leaves a room group (client-side only, does not remove from DB).
    /// </summary>
    public async Task LeaveRoom(Guid roomId)
    {
        var userId = GetUserReferenceId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{roomId}");
        _logger.LogInformation("User {UserId} left room {RoomId}", userId, roomId);
    }

    private Guid GetUserReferenceId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? Context.User?.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;

        _logger.LogError(
            "GetUserReferenceId failed — JWT has no valid 'sub' claim. " +
            "ConnectionId: {ConnectionId}, Claims: [{Claims}]",
            Context.ConnectionId,
            string.Join(", ", Context.User?.Claims.Select(c => $"{c.Type}={c.Value}") ?? []));

        throw new HubException(
            "Unauthorized: JWT is missing a valid user identifier ('sub' claim). " +
            "Make sure you are using a token issued by backend-isj.");
    }
}
