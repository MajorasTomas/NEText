using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEText.Api.Auth;
using NEText.Api.Dtos;
using NEText.Api.Services;

namespace NEText.Api.Controllers;

// Everything here acts as the logged-in user: User.GetUserId() = "me".
[ApiController]
[Authorize]
[Route("api/friend-requests")]
public class FriendRequestsController : ControllerBase
{
    private readonly IFriendService _friends;

    public FriendRequestsController(IFriendService friends)
    {
        _friends = friends;
    }

    // POST /api/friend-requests  { "toUserId": "..." } -> 201 + request
    [HttpPost]
    public async Task<ActionResult<FriendRequestDto>> Send([FromBody] SendFriendRequestRequest request)
    {
        var created = await _friends.SendRequestAsync(User.GetUserId(), request.ToUserId!.Value);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    // GET /api/friend-requests/incoming -> 200 + pending requests sent to me
    [HttpGet("incoming")]
    public async Task<ActionResult<List<FriendRequestDto>>> Incoming()
    {
        return Ok(await _friends.GetIncomingAsync(User.GetUserId()));
    }

    // GET /api/friend-requests/outgoing -> 200 + pending requests I sent
    [HttpGet("outgoing")]
    public async Task<ActionResult<List<FriendRequestDto>>> Outgoing()
    {
        return Ok(await _friends.GetOutgoingAsync(User.GetUserId()));
    }

    // POST /api/friend-requests/{id}/accept -> 200 + request (now Accepted)
    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<FriendRequestDto>> Accept(Guid id)
    {
        return Ok(await _friends.AcceptAsync(User.GetUserId(), id));
    }

    // POST /api/friend-requests/{id}/decline -> 204
    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id)
    {
        await _friends.DeclineAsync(User.GetUserId(), id);
        return NoContent();
    }

    // DELETE /api/friend-requests/{id} -> 204 (sender cancels their pending request)
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _friends.CancelAsync(User.GetUserId(), id);
        return NoContent();
    }
}
