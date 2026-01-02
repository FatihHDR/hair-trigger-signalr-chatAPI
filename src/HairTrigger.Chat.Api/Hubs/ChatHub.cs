using HairTrigger.Chat.Domain.Queue;
using HairTrigger.Chat.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace HairTrigger.Chat.Api.Hubs;

public class ChatHub : Hub
{
    private readonly IMessageQueue _messageQueue;
    private readonly IChannelRepository _channelRepository;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IMessageQueue messageQueue,
        IChannelRepository channelRepository,
        ILogger<ChatHub> logger)
    {
        _messageQueue = messageQueue;
        _channelRepository = channelRepository;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} connected with ConnectionId {ConnectionId}", userId, Context.ConnectionId);

        // Join user to their personal group for direct messages
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        // Get user's channels and join those groups
        var channels = await _channelRepository.GetUserChannelsAsync(userId);
        foreach (var channel in channels)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channel.Id}");
            _logger.LogDebug("User {UserId} joined channel group {ChannelId}", userId, channel.Id);
        }

        // Enqueue connected event for worker
        await _messageQueue.EnqueueAsync(new UserConnectedCommand(
            UserId: userId,
            ConnectionId: Context.ConnectionId,
            EnqueuedAt: DateTime.UtcNow
        ));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} disconnected. Exception: {Exception}", userId, exception?.Message);

        // Enqueue disconnected event for worker
        await _messageQueue.EnqueueAsync(new UserDisconnectedCommand(
            UserId: userId,
            ConnectionId: Context.ConnectionId,
            EnqueuedAt: DateTime.UtcNow
        ));

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Sends a message to a channel. The hub validates and enqueues the command for the worker.
    /// </summary>
    public async Task SendMessageToChannel(Guid channelId, string content)
    {
        var userId = GetUserId();
        
        // Validate channel membership
        var isMember = await _channelRepository.IsMemberAsync(channelId, userId);
        if (!isMember)
        {
            _logger.LogWarning("User {UserId} attempted to send message to channel {ChannelId} without membership", userId, channelId);
            throw new HubException("You are not a member of this channel");
        }

        // Validate content
        if (string.IsNullOrWhiteSpace(content) || content.Length > 4000)
        {
            throw new HubException("Message content is invalid or too long");
        }

        // Enqueue message command for worker to process
        await _messageQueue.EnqueueAsync(new SendMessageCommand(
            ChannelId: channelId,
            SenderId: userId,
            Content: content.Trim(),
            ConnectionId: Context.ConnectionId,
            EnqueuedAt: DateTime.UtcNow
        ));

        _logger.LogDebug("Message queued from user {UserId} to channel {ChannelId}", userId, channelId);
    }

    /// <summary>
    /// Marks messages as seen up to a specific offset in a channel.
    /// </summary>
    public async Task MarkSeen(Guid channelId, long lastSeenOffset)
    {
        var userId = GetUserId();

        // Validate channel membership
        var isMember = await _channelRepository.IsMemberAsync(channelId, userId);
        if (!isMember)
        {
            throw new HubException("You are not a member of this channel");
        }

        // Enqueue mark seen command for worker
        await _messageQueue.EnqueueAsync(new MarkSeenCommand(
            ChannelId: channelId,
            UserId: userId,
            LastSeenOffset: lastSeenOffset,
            EnqueuedAt: DateTime.UtcNow
        ));

        _logger.LogDebug("Mark seen queued for user {UserId} in channel {ChannelId} up to offset {Offset}", 
            userId, channelId, lastSeenOffset);
    }

    /// <summary>
    /// Joins a channel group (called after being added to a channel by admin/owner)
    /// </summary>
    public async Task JoinChannel(Guid channelId)
    {
        var userId = GetUserId();

        var isMember = await _channelRepository.IsMemberAsync(channelId, userId);
        if (!isMember)
        {
            throw new HubException("You are not a member of this channel");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}");
        _logger.LogInformation("User {UserId} joined channel {ChannelId}", userId, channelId);
    }

    /// <summary>
    /// Leaves a channel group
    /// </summary>
    public async Task LeaveChannel(Guid channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");
        _logger.LogInformation("User {UserId} left channel {ChannelId}", GetUserId(), channelId);
    }

    private Guid GetUserId()
    {
        // Try to get user ID from claims (when authenticated)
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? Context.User?.FindFirst("sub")?.Value;
        
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        // For development/testing: use a query parameter or header
        var httpContext = Context.GetHttpContext();
        var queryUserId = httpContext?.Request.Query["userId"].FirstOrDefault();
        
        if (Guid.TryParse(queryUserId, out var queryGuid))
        {
            return queryGuid;
        }

        // Fallback: generate a temporary ID (not recommended for production)
        _logger.LogWarning("No user ID found in claims, generating temporary ID for connection {ConnectionId}", Context.ConnectionId);
        return Guid.NewGuid();
    }
}
