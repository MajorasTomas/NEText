using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEText.Api.Auth;
using NEText.Api.Dtos;
using NEText.Api.Services;

namespace NEText.Api.Controllers;

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

    // POST /api/auth/login -> 200 + { accessToken, expiresAtUtc, user }
    //                         401 wrong email or password (UnauthorizedException)
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        return Ok(await _users.LoginAsync(request));
    }

    // GET /api/auth/me -> 200 + the logged-in user (including their email)
    //                     401 if there is no valid token (handled by the JWT middleware before this runs)
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        // "User" = the claims from the validated token; GetUserId() reads the "sub" claim.
        return Ok(await _users.GetCurrentUserAsync(User.GetUserId()));
    }
}
