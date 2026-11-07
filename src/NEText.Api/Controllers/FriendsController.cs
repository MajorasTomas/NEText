using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEText.Api.Auth;
using NEText.Api.Dtos;
using NEText.Api.Services;

namespace NEText.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/friends")]
public class FriendsController : ControllerBase
{
    private readonly IFriendService _friends;

    public FriendsController(IFriendService friends)
    {
        _friends = friends;
    }

    [HttpGet]
    public async Task<ActionResult<List<FriendDto>>> GetAll()
    {
        return Ok(await _friends.GetFriendsAsync(User.GetUserId()));
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Remove(Guid userId)
    {
        await _friends.RemoveFriendAsync(User.GetUserId(), userId);
        return NoContent();
    }
}
