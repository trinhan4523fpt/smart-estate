using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.Messages;
using SmartEstate.App.Features.Messages.Dtos;
using SmartEstate.Shared.Results;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly MessageService _svc;

    public MessagesController(MessageService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Start a new conversation or get existing one.
    /// </summary>
    [HttpPost("conversations")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> StartConversation([FromBody] StartConversationRequest req, CancellationToken ct)
    {
        var result = await _svc.StartConversationAsync(req, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(new { ConversationId = result.Value });
    }

    /// <summary>
    /// Get list of conversations for current user.
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(List<ConversationDto>), 200)]
    public async Task<IActionResult> GetConversations(CancellationToken ct)
    {
        var result = await _svc.GetConversationsAsync(ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Send a message in a conversation.
    /// </summary>
    [HttpPost("conversations/{id:guid}")]
    [ProducesResponseType(typeof(MessageDto), 200)]
    public async Task<IActionResult> SendMessage([FromRoute] Guid id, [FromBody] SendMessageRequest req, CancellationToken ct)
    {
        var result = await _svc.SendMessageAsync(id, req, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Get messages of a conversation.
    /// </summary>
    [HttpGet("conversations/{id:guid}")]
    [ProducesResponseType(typeof(List<MessageDto>), 200)]
    public async Task<IActionResult> GetMessages([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.GetMessagesAsync(id, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Mark conversation as read for current user.
    /// </summary>
    [HttpPost("conversations/{id:guid}/read")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> MarkConversationRead([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.MarkConversationReadAsync(id, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok();
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var result = await _svc.GetUnreadCountAsync(ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(new { Count = result.Value });
    }
}
