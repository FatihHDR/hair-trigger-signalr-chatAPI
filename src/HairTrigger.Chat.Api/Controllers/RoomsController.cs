using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HairTrigger.Chat.Api.Controllers;

[ApiController]
[Route("api/v1/rooms")]
[Produces("application/json")]
public class RoomsController : ControllerBase
{
    private readonly IChatRoomRepository _chatRoomRepository;

    public RoomsController(IChatRoomRepository chatRoomRepository)
    {
        _chatRoomRepository = chatRoomRepository;
    }

    /// <summary>
    /// Get all active chat rooms
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ChatRoomDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChatRoomDto>>> GetRooms()
    {
        var rooms = await _chatRoomRepository.GetActiveRoomsAsync();
        return Ok(rooms.Select(ToDto));
    }

    /// <summary>
    /// Get chat room by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ChatRoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatRoomDto>> GetRoom(Guid id)
    {
        var room = await _chatRoomRepository.GetByIdAsync(id);
        if (room == null)
            return NotFound();
        
        return Ok(ToDto(room));
    }

    /// <summary>
    /// Get rooms for a specific user (by user reference ID from primary DB)
    /// </summary>
    [HttpGet("user/{userReferenceId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ChatRoomDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChatRoomDto>>> GetUserRooms(Guid userReferenceId)
    {
        var rooms = await _chatRoomRepository.GetUserRoomsAsync(userReferenceId);
        return Ok(rooms.Select(ToDto));
    }

    /// <summary>
    /// Create a new chat room
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChatRoomDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ChatRoomDto>> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var room = new ChatRoom
        {
            Id = Guid.NewGuid(),
            RoomType = request.RoomType,
            SessionReferenceId = request.SessionReferenceId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _chatRoomRepository.CreateAsync(room);
        return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, ToDto(room));
    }

    /// <summary>
    /// Close a chat room
    /// </summary>
    [HttpPost("{roomId:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseRoom(Guid roomId)
    {
        var room = await _chatRoomRepository.GetByIdAsync(roomId);
        if (room == null)
            return NotFound();

        await _chatRoomRepository.CloseRoomAsync(roomId);
        return NoContent();
    }

    /// <summary>
    /// Add a participant to a chat room
    /// </summary>
    [HttpPost("{roomId:guid}/participants")]
    [ProducesResponseType(typeof(ParticipantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantDto>> AddParticipant(Guid roomId, [FromBody] AddParticipantRequest request)
    {
        var room = await _chatRoomRepository.GetByIdAsync(roomId);
        if (room == null)
            return NotFound("Room not found");

        var participant = await _chatRoomRepository.AddParticipantAsync(roomId, request.UserReferenceId, request.Role);
        return Created("", ToParticipantDto(participant));
    }

    /// <summary>
    /// Remove a participant from a chat room
    /// </summary>
    [HttpDelete("{roomId:guid}/participants/{userReferenceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveParticipant(Guid roomId, Guid userReferenceId)
    {
        await _chatRoomRepository.RemoveParticipantAsync(roomId, userReferenceId);
        return NoContent();
    }

    /// <summary>
    /// Get participants of a chat room
    /// </summary>
    [HttpGet("{roomId:guid}/participants")]
    [ProducesResponseType(typeof(IEnumerable<ParticipantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ParticipantDto>>> GetParticipants(Guid roomId)
    {
        var room = await _chatRoomRepository.GetByIdAsync(roomId);
        if (room == null)
            return NotFound();

        var participants = await _chatRoomRepository.GetParticipantsAsync(roomId);
        return Ok(participants.Select(ToParticipantDto));
    }

    private static ChatRoomDto ToDto(ChatRoom room) => 
        new(room.Id, room.RoomType.ToString(), room.SessionReferenceId, room.IsActive, room.ClosedAt, room.CreatedAt);

    private static ParticipantDto ToParticipantDto(ChatRoomParticipant p) =>
        new(p.Id, p.RoomId, p.UserReferenceId, p.Role, p.JoinedAt, p.LeftAt);
}

public record ChatRoomDto(Guid Id, string RoomType, Guid? SessionReferenceId, bool IsActive, DateTime? ClosedAt, DateTime CreatedAt);
public record CreateRoomRequest(ChatRoomType RoomType, Guid? SessionReferenceId);
public record AddParticipantRequest(Guid UserReferenceId, string Role);
public record ParticipantDto(Guid Id, Guid RoomId, Guid UserReferenceId, string Role, DateTime JoinedAt, DateTime? LeftAt);
