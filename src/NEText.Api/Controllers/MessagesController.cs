using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEText.Api.Auth;
using NEText.Api.Dtos;
using NEText.Api.Services;

namespace NEText.Api.Controllers;

// Actions on a single message (sending happens under /api/conversations/{id}/messages).
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

    // PUT /api/messages/{id}  { "text": "fixed" } -> 200 + edited message
    // PUT = replace the editable content (the text) with what's sent.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MessageDto>> Edit(Guid id, [FromBody] EditMessageRequest request)
    {
        return Ok(await _conversations.EditMessageAsync(User.GetUserId(), id, request.Text));
    }

    // DELETE /api/messages/{id} -> 204 (soft delete: shown as "message deleted")
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _conversations.DeleteMessageAsync(User.GetUserId(), id);
        return NoContent();
    }
}
