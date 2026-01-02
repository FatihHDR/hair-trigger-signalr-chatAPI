using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HairTrigger.Chat.Api.Controllers;

[ApiController]
[Route("api/v1/messages")]
[Produces("application/json")]
public class MessagesController : ControllerBase
{
    private readonly IMessageRepository _messageRepository;
    private readonly IDeliveryStatusRepository _deliveryStatusRepository;

    public MessagesController(
        IMessageRepository messageRepository,
        IDeliveryStatusRepository deliveryStatusRepository)
    {
        _messageRepository = messageRepository;
        _deliveryStatusRepository = deliveryStatusRepository;
    }

    /// <summary>
    /// Get messages from a channel
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <param name="fromOffset">Get messages after this offset (optional)</param>
    /// <param name="take">Number of messages to retrieve (default: 50)</param>
    [HttpGet("channel/{channelId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<MessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetChannelMessages(
        Guid channelId,
        [FromQuery] long? fromOffset = null,
        [FromQuery] int take = 50)
    {
        var messages = await _messageRepository.GetChannelMessagesAsync(channelId, fromOffset, take);
        return Ok(messages.Select(ToDto));
    }

    /// <summary>
    /// Get a specific message by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> GetMessage(Guid id)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        if (message == null)
            return NotFound();
        
        return Ok(ToDto(message));
    }

    /// <summary>
    /// Get delivery status for a message
    /// </summary>
    [HttpGet("{messageId:guid}/status")]
    [ProducesResponseType(typeof(IEnumerable<DeliveryStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DeliveryStatusDto>>> GetDeliveryStatus(Guid messageId)
    {
        var statuses = await _deliveryStatusRepository.GetForMessageAsync(messageId);
        return Ok(statuses.Select(s => new DeliveryStatusDto(
            s.UserId,
            s.MessageId,
            s.DeliveredAt,
            s.SeenAt
        )));
    }

    /// <summary>
    /// Delete a message (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMessage(Guid id)
    {
        await _messageRepository.DeleteMessageAsync(id);
        return NoContent();
    }

    private static MessageDto ToDto(Message message) => new(
        message.Id,
        message.ChannelId,
        message.SenderId,
        message.Sender?.DisplayName ?? "Unknown",
        message.Content,
        message.Offset,
        message.CreatedAt
    );
}

public record MessageDto(
    Guid Id,
    Guid ChannelId,
    Guid SenderId,
    string SenderName,
    string Content,
    long Offset,
    DateTime CreatedAt
);

public record DeliveryStatusDto(
    Guid UserId,
    Guid MessageId,
    DateTime? DeliveredAt,
    DateTime? SeenAt
);
