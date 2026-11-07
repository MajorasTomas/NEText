using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEText.Api.Auth;
using NEText.Api.Dtos;
using NEText.Api.Services;

namespace NEText.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/messages")]
public class MessagesController : ControllerBase
{
    private readonly IConversationService _conversations;

    public MessagesController(IConversationService conversations)
    {
        _conversations = conversations;
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MessageDto>> Edit(Guid id, [FromBody] EditMessageRequest request)
    {
        return Ok(await _conversations.EditMessageAsync(User.GetUserId(), id, request.Text));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _conversations.DeleteMessageAsync(User.GetUserId(), id);
        return NoContent();
    }
}
