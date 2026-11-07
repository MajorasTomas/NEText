using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEText.Api.Auth;
using NEText.Api.Dtos;
using NEText.Api.Services;

namespace NEText.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversations;

    public ConversationsController(IConversationService conversations)
    {
        _conversations = conversations;
    }

    // POST /api/conversations  { "userId": "..." } -> 201 new conversation, or 200 if it already existed
    [HttpPost]
    public async Task<ActionResult<ConversationDto>> Start([FromBody] StartConversationRequest request)
    {
        var (conversation, created) = await _conversations.StartAsync(User.GetUserId(), request.UserId!.Value);
        return created ? StatusCode(StatusCodes.Status201Created, conversation) : Ok(conversation);
    }

    // GET /api/conversations -> 200 + my conversations, most recent first
    [HttpGet]
    public async Task<ActionResult<List<ConversationDto>>> GetMine()
    {
        return Ok(await _conversations.GetMyConversationsAsync(User.GetUserId()));
    }

    // GET /api/conversations/{id} -> 200, or 404 if it isn't mine
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDto>> Get(Guid id)
    {
        return Ok(await _conversations.GetAsync(User.GetUserId(), id));
    }

    // POST /api/conversations/{id}/read -> 204 (unread count becomes 0)
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _conversations.MarkAsReadAsync(User.GetUserId(), id);
        return NoContent();
    }

    // GET /api/conversations/{id}/messages?limit=50&before={messageId} -> 200 + messages, newest first
    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(
        Guid id,
        [FromQuery] Guid? before,
        [FromQuery, Range(1, 100)] int limit = 50)
    {
        return Ok(await _conversations.GetMessagesAsync(User.GetUserId(), id, before, limit));
    }

    // POST /api/conversations/{id}/messages  { "text": "hi" } -> 201 + message
    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(Guid id, [FromBody] SendMessageRequest request)
    {
        var message = await _conversations.SendMessageAsync(User.GetUserId(), id, request.Text);
        return StatusCode(StatusCodes.Status201Created, message);
    }
}
