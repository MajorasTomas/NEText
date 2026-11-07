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

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request)
    {
        var user = await _users.RegisterAsync(request);
        return CreatedAtAction(nameof(UsersController.GetById), "Users", new { id = user.Id }, user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        return Ok(await _users.LoginAsync(request));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        return Ok(await _users.GetCurrentUserAsync(User.GetUserId()));
    }
}
