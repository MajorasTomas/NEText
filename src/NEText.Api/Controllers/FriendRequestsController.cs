using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEText.Api.Auth;
using NEText.Api.Dtos;
using NEText.Api.Services;

namespace NEText.Api.Controllers;

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

    [HttpPost]
    public async Task<ActionResult<FriendRequestDto>> Send([FromBody] SendFriendRequestRequest request)
    {
        var created = await _friends.SendRequestAsync(User.GetUserId(), request.ToUserId!.Value);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet("incoming")]
    public async Task<ActionResult<List<FriendRequestDto>>> Incoming()
    {
        return Ok(await _friends.GetIncomingAsync(User.GetUserId()));
    }

    [HttpGet("outgoing")]
    public async Task<ActionResult<List<FriendRequestDto>>> Outgoing()
    {
        return Ok(await _friends.GetOutgoingAsync(User.GetUserId()));
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<FriendRequestDto>> Accept(Guid id)
    {
        return Ok(await _friends.AcceptAsync(User.GetUserId(), id));
    }

    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id)
    {
        await _friends.DeclineAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _friends.CancelAsync(User.GetUserId(), id);
        return NoContent();
    }
}
