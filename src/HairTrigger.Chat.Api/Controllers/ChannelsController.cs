using HairTrigger.Chat.Domain.Entities;
using HairTrigger.Chat.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HairTrigger.Chat.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChannelsController : ControllerBase
{
    private readonly IChannelRepository _channelRepository;
    private readonly IUserRepository _userRepository;

    public ChannelsController(IChannelRepository channelRepository, IUserRepository userRepository)
    {
        _channelRepository = channelRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Get all active channels
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ChannelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChannelDto>>> GetChannels()
    {
        var channels = await _channelRepository.GetActiveChannelsAsync();
        return Ok(channels.Select(ToDto));
    }

    /// <summary>
    /// Get channel by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ChannelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChannelDto>> GetChannel(Guid id)
    {
        var channel = await _channelRepository.GetByIdAsync(id);
        if (channel == null)
            return NotFound();
        
        return Ok(ToDto(channel));
    }

    /// <summary>
    /// Get channels for a specific user
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ChannelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChannelDto>>> GetUserChannels(Guid userId)
    {
        var channels = await _channelRepository.GetUserChannelsAsync(userId);
        return Ok(channels.Select(ToDto));
    }

    /// <summary>
    /// Create a new channel
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChannelDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ChannelDto>> CreateChannel([FromBody] CreateChannelRequest request)
    {
        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _channelRepository.CreateAsync(channel);
        return CreatedAtAction(nameof(GetChannel), new { id = channel.Id }, ToDto(channel));
    }

    /// <summary>
    /// Add a member to a channel
    /// </summary>
    [HttpPost("{channelId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMember(Guid channelId, Guid userId)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId);
        if (channel == null)
            return NotFound("Channel not found");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        await _channelRepository.AddMemberAsync(channelId, userId);
        return NoContent();
    }

    /// <summary>
    /// Remove a member from a channel
    /// </summary>
    [HttpDelete("{channelId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMember(Guid channelId, Guid userId)
    {
        await _channelRepository.RemoveMemberAsync(channelId, userId);
        return NoContent();
    }

    /// <summary>
    /// Get members of a channel
    /// </summary>
    [HttpGet("{channelId:guid}/members")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetMembers(Guid channelId)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId);
        if (channel == null)
            return NotFound();

        var users = await _userRepository.GetUsersInChannelAsync(channelId);
        return Ok(users.Select(u => new UserDto(u.Id, u.Username, u.DisplayName, u.CreatedAt)));
    }

    private static ChannelDto ToDto(Channel channel) => 
        new(channel.Id, channel.Name, channel.Description, channel.CreatedAt, channel.IsActive);
}

public record ChannelDto(Guid Id, string Name, string? Description, DateTime CreatedAt, bool IsActive);
public record CreateChannelRequest(string Name, string? Description);
