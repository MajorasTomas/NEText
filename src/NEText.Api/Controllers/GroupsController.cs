using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEText.Api.Auth;
using NEText.Api.Dtos;
using NEText.Api.Entities;
using NEText.Api.Services;

namespace NEText.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/groups")]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groups;

    public GroupsController(IGroupService groups)
    {
        _groups = groups;
    }

    [HttpPost]
    public async Task<ActionResult<ConversationDto>> Create([FromBody] CreateGroupRequest request)
    {
        var group = await _groups.CreateAsync(User.GetUserId(), request.Name, request.MemberIds);
        return StatusCode(StatusCodes.Status201Created, group);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ConversationDto>> Rename(Guid id, [FromBody] RenameGroupRequest request)
    {
        return Ok(await _groups.RenameAsync(User.GetUserId(), id, request.Name));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _groups.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<List<GroupMemberDto>>> Members(Guid id, [FromQuery] ParticipantRole? role)
    {
        return Ok(await _groups.GetMembersAsync(User.GetUserId(), id, role));
    }

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<GroupMemberDto>> AddMember(Guid id, [FromBody] AddGroupMemberRequest request)
    {
        var member = await _groups.AddMemberAsync(User.GetUserId(), id, request.UserId!.Value);
        return StatusCode(StatusCodes.Status201Created, member);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        await _groups.RemoveMemberAsync(User.GetUserId(), id, userId);
        return NoContent();
    }

    [HttpPut("{id:guid}/members/{userId:guid}/role")]
    public async Task<ActionResult<GroupMemberDto>> ChangeRole(Guid id, Guid userId, [FromBody] ChangeRoleRequest request)
    {
        return Ok(await _groups.ChangeRoleAsync(User.GetUserId(), id, userId, request.Role!.Value));
    }

    [HttpPut("{id:guid}/members/me/nickname")]
    public async Task<ActionResult<GroupMemberDto>> SetMyNickname(Guid id, [FromBody] SetNicknameRequest request)
    {
        return Ok(await _groups.SetMyNicknameAsync(User.GetUserId(), id, request.Nickname));
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id)
    {
        await _groups.LeaveAsync(User.GetUserId(), id);
        return NoContent();
    }
}
