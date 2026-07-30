using HairTrigger.Chat.Api.Hubs;
using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using HairTrigger.Chat.Domain.Queue;
using Microsoft.AspNetCore.SignalR;

namespace HairTrigger.Chat.Api;

/// <summary>
/// Background service that runs inside the API process.
/// Dequeues SendMessageCommand, persists the message to DB,
/// then broadcasts to the SignalR room group via IHubContext.
///
/// MUST run in the same process as ChatHub so IHubContext
/// resolves to the actual hub and reaches connected WebSocket clients.
/// </summary>
public class MessageWorker : BackgroundService
{
    private readonly ILogger<MessageWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageQueue _messageQueue;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessageWorker(
        ILogger<MessageWorker> logger,
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
        _logger.LogInformation("ISJ Chat MessageWorker started at: {time}", DateTimeOffset.Now);

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
                            // Reserved for presence tracking
                            break;
                    }

                    continue;
                }

                // No commands in queue — wait briefly before polling again
                await Task.Delay(100, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in MessageWorker execution");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("ISJ Chat MessageWorker stopped at: {time}", DateTimeOffset.Now);
    }

    private async Task ProcessSendMessageAsync(SendMessageCommand command, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var chatMessageRepository = scope.ServiceProvider.GetRequiredService<IChatMessageRepository>();

        try
        {
            // Persist message to DB
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

            // Broadcast to all connections in this room's SignalR group
            await _hubContext.Clients
                .Group($"room:{command.RoomId}")
                .SendAsync("ReceiveMessage", new
                {
                    message.Id,
                    message.RoomId,
                    message.SenderReferenceId,
                    MessageType = message.MessageType.ToString(),
                    message.Content,
                    message.CreatedAt
                }, cancellationToken);

            _logger.LogDebug(
                "Message {MessageId} persisted and broadcast to room:{RoomId}",
                message.Id, command.RoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process SendMessageCommand for room {RoomId}",
                command.RoomId);
        }
    }
}
