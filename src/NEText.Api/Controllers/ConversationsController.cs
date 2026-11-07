using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEText.Api.Auth;
using NEText.Api.Dtos;
using NEText.Api.Entities;
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

    [HttpPost]
    public async Task<ActionResult<ConversationDto>> Start([FromBody] StartConversationRequest request)
    {
        var (conversation, created) = await _conversations.StartAsync(User.GetUserId(), request.UserId!.Value);
        return created ? StatusCode(StatusCodes.Status201Created, conversation) : Ok(conversation);
    }

    [HttpGet]
    public async Task<ActionResult<List<ConversationDto>>> GetMine([FromQuery] ConversationType? type)
    {
        return Ok(await _conversations.GetMyConversationsAsync(User.GetUserId(), type));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDto>> Get(Guid id)
    {
        return Ok(await _conversations.GetAsync(User.GetUserId(), id));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _conversations.MarkAsReadAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(Guid id, [FromQuery] MessageListQuery query)
    {
        return Ok(await _conversations.GetMessagesAsync(User.GetUserId(), id, query));
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(Guid id, [FromBody] SendMessageRequest request)
    {
        var message = await _conversations.SendMessageAsync(User.GetUserId(), id, request.Text);
        return StatusCode(StatusCodes.Status201Created, message);
    }
}
