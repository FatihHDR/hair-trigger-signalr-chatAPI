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
        _logger.LogInformation("ISJ Chat Worker started at: {time}", DateTimeOffset.Now);

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

        _logger.LogInformation("ISJ Chat Worker stopped at: {time}", DateTimeOffset.Now);
    }

    private async Task ProcessSendMessageAsync(SendMessageCommand command, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var chatMessageRepository = scope.ServiceProvider.GetRequiredService<IChatMessageRepository>();

        try
        {
            // Create and persist message
            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                RoomId = command.RoomId,
                SenderReferenceId = command.SenderReferenceId,
                MessageType = MessageType.Text,
                Content = command.Content,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await chatMessageRepository.AddMessageAsync(message);

            // Broadcast to room via Redis pub/sub
            await BroadcastToRoomAsync(command.RoomId, "ReceiveMessage", new
            {
                message.Id,
                message.RoomId,
                message.SenderReferenceId,
                MessageType = message.MessageType.ToString(),
                message.Content,
                message.CreatedAt
            });

            _logger.LogDebug("Message {MessageId} persisted and broadcast to room {RoomId}", message.Id, command.RoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process SendMessageCommand for room {RoomId}", command.RoomId);
        }
    }

    private async Task BroadcastToRoomAsync(Guid roomId, string method, object data)
    {
        await _hubContext.Clients.Group($"room:{roomId}").SendAsync(method, data);
    }
}
