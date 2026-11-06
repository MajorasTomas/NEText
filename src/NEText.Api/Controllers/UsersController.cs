using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEText.Api.Dtos;
using NEText.Api.Services;

namespace NEText.Api.Controllers;

// [Authorize] on the class = every endpoint here requires a valid token (otherwise 401).
[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    // GET /api/users/{id} -> 200 + user, 404 if not found (NotFoundException)
    // {id:guid} = only matches if that part of the URL is a valid GUID
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        return Ok(await _users.GetByIdAsync(id));
    }

    // GET /api/users/search?q=ali -> 200 + matching users (possibly an empty list)
    [HttpGet("search")]
    public async Task<ActionResult<List<UserDto>>> Search([FromQuery, Required, MinLength(2), MaxLength(50)] string q)
    {
        return Ok(await _users.SearchAsync(q));
    }
}
