using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HairTrigger.Chat.Api.Controllers;

[ApiController]
[Route("api/v1/messages")]
[Produces("application/json")]
public class MessagesController : ControllerBase
{
    private readonly IChatMessageRepository _chatMessageRepository;

    public MessagesController(IChatMessageRepository chatMessageRepository)
    {
        _chatMessageRepository = chatMessageRepository;
    }

    /// <summary>
    /// Get messages from a chat room
    /// </summary>
    /// <param name="roomId">Room ID</param>
    /// <param name="before">Get messages before this timestamp (cursor pagination)</param>
    /// <param name="take">Number of messages to retrieve (default: 50)</param>
    [HttpGet("room/{roomId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ChatMessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetRoomMessages(
        Guid roomId,
        [FromQuery] DateTime? before = null,
        [FromQuery] int take = 50)
    {
        var messages = await _chatMessageRepository.GetRoomMessagesAsync(roomId, take, before);
        return Ok(messages.Select(ToDto));
    }

    /// <summary>
    /// Get a specific message by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatMessageDto>> GetMessage(Guid id)
    {
        var message = await _chatMessageRepository.GetByIdAsync(id);
        if (message == null)
            return NotFound();
        
        return Ok(ToDto(message));
    }

    /// <summary>
    /// Delete a message (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMessage(Guid id)
    {
        await _chatMessageRepository.DeleteMessageAsync(id);
        return NoContent();
    }

    private static ChatMessageDto ToDto(ChatMessage message) => new(
        message.Id,
        message.RoomId,
        message.SenderReferenceId,
        message.MessageType.ToString(),
        message.Content,
        message.CreatedAt
    );
}

public record ChatMessageDto(
    Guid Id,
    Guid RoomId,
    Guid SenderReferenceId,
    string MessageType,
    string Content,
    DateTime CreatedAt
);
