using Microsoft.AspNetCore.Mvc;
using NEText.Api.Dtos;
using NEText.Api.Services;

namespace NEText.Api.Controllers;

// Registration now; login (JWT) comes in the next step.
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;

    public AuthController(IUserService users)
    {
        _users = users;
    }

    // POST /api/auth/register -> 201 + user (Location: /api/users/{id})
    //                            400 invalid input (automatic), 409 username/email taken (ConflictException)
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request)
    {
        var user = await _users.RegisterAsync(request);
        return CreatedAtAction(nameof(UsersController.GetById), "Users", new { id = user.Id }, user);
    }
}
