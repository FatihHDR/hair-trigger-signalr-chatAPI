using HairTrigger.Chat.Api.Hubs;
using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using HairTrigger.Chat.Domain.Queue;
using Microsoft.AspNetCore.SignalR;

namespace HairTrigger.Chat.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageQueue _messageQueue;
    private readonly IHubContext<ChatHub> _hubContext;

    public Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        IMessageQueue messageQueue,
        IHubContext<ChatHub> hubContext)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _messageQueue = messageQueue;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HairTrigger Chat Worker started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var command = await _messageQueue.DequeueAsync(stoppingToken);
                if (command != null)
                {
                    switch (command)
                    {
                        case SendMessageCommand sendMessageCmd:
                            await ProcessSendMessageAsync(sendMessageCmd, stoppingToken);
                            break;
                        case MarkSeenCommand markSeenCmd:
                            await ProcessMarkSeenAsync(markSeenCmd, stoppingToken);
                            break;
                        case UserConnectedCommand:
                        case UserDisconnectedCommand:
                            // Reserved for presence tracking.
                            break;
                    }

                    continue;
                }

                // No commands in queue, wait briefly
                await Task.Delay(100, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Worker execution");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("HairTrigger Chat Worker stopped at: {time}", DateTimeOffset.Now);
    }

    private async Task ProcessSendMessageAsync(SendMessageCommand command, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var messageRepository = scope.ServiceProvider.GetRequiredService<IMessageRepository>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        try
        {
            // Get next offset for channel
            var offset = await messageRepository.GetNextOffsetAsync(command.ChannelId);

            // Create and persist message
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ChannelId = command.ChannelId,
                SenderId = command.SenderId,
                Content = command.Content,
                Offset = offset,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await messageRepository.AddMessageAsync(message);

            // Get sender info
            var sender = await userRepository.GetByIdAsync(command.SenderId);

            // Broadcast to channel via Redis pub/sub
            await BroadcastToChannelAsync(command.ChannelId, "ReceiveMessage", new
            {
                message.Id,
                message.ChannelId,
                message.SenderId,
                SenderName = sender?.DisplayName ?? "Unknown",
                message.Content,
                message.Offset,
                message.CreatedAt
            });

            _logger.LogDebug("Message {MessageId} persisted and broadcast to channel {ChannelId}", message.Id, command.ChannelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process SendMessageCommand for channel {ChannelId}", command.ChannelId);
        }
    }

    private async Task ProcessMarkSeenAsync(MarkSeenCommand command, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var deliveryStatusRepository = scope.ServiceProvider.GetRequiredService<IDeliveryStatusRepository>();

        try
        {
            await deliveryStatusRepository.MarkSeenUpToOffsetAsync(
                command.UserId,
                command.ChannelId,
                command.LastSeenOffset);

            // Broadcast read receipt to channel
            await BroadcastToChannelAsync(command.ChannelId, "MessageSeen", new
            {
                command.UserId,
                command.ChannelId,
                command.LastSeenOffset,
                SeenAt = DateTime.UtcNow
            });

            _logger.LogDebug("Mark seen processed for user {UserId} in channel {ChannelId} up to offset {Offset}",
                command.UserId, command.ChannelId, command.LastSeenOffset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process MarkSeenCommand for user {UserId}", command.UserId);
        }
    }

    private async Task BroadcastToChannelAsync(Guid channelId, string method, object data)
    {
        await _hubContext.Clients.Group($"channel:{channelId}").SendAsync(method, data);
    }
}
