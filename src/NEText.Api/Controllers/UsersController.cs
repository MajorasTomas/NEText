using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEText.Api.Dtos;
using NEText.Api.Services;

namespace NEText.Api.Controllers;

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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        return Ok(await _users.GetByIdAsync(id));
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<UserDto>>> Search([FromQuery, Required, MinLength(2), MaxLength(50)] string q)
    {
        return Ok(await _users.SearchAsync(q));
    }
}
