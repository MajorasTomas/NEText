using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEText.Api.Auth;
using NEText.Api.Dtos;
using NEText.Api.Entities;
using NEText.Api.Services;

namespace NEText.Api.Controllers;

// Group management. A group is a conversation, so reading/sending messages, marking as read and
// listing ("my conversations") use the /api/conversations endpoints with the group's id.
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

    // POST /api/groups  { "name": "...", "memberIds": ["..."] } -> 201 + conversation
    [HttpPost]
    public async Task<ActionResult<ConversationDto>> Create([FromBody] CreateGroupRequest request)
    {
        var group = await _groups.CreateAsync(User.GetUserId(), request.Name, request.MemberIds);
        return StatusCode(StatusCodes.Status201Created, group);
    }

    // PATCH /api/groups/{id}  { "name": "..." } -> 200 (admin)
    // PATCH = change only some fields of the group (here: just the name).
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ConversationDto>> Rename(Guid id, [FromBody] RenameGroupRequest request)
    {
        return Ok(await _groups.RenameAsync(User.GetUserId(), id, request.Name));
    }

    // DELETE /api/groups/{id} -> 204 (admin; deletes all messages too)
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _groups.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }

    // GET /api/groups/{id}/members            -> 200 + all members (admins first)
    // GET /api/groups/{id}/members?role=Admin -> only admins (or ?role=Member for only members)
    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<List<GroupMemberDto>>> Members(Guid id, [FromQuery] ParticipantRole? role)
    {
        return Ok(await _groups.GetMembersAsync(User.GetUserId(), id, role));
    }

    // POST /api/groups/{id}/members  { "userId": "..." } -> 201 + member (admin; must be my friend)
    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<GroupMemberDto>> AddMember(Guid id, [FromBody] AddGroupMemberRequest request)
    {
        var member = await _groups.AddMemberAsync(User.GetUserId(), id, request.UserId!.Value);
        return StatusCode(StatusCodes.Status201Created, member);
    }

    // DELETE /api/groups/{id}/members/{userId} -> 204 (admin)
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        await _groups.RemoveMemberAsync(User.GetUserId(), id, userId);
        return NoContent();
    }

    // PUT /api/groups/{id}/members/{userId}/role  { "role": "Admin" } -> 200 (admin)
    [HttpPut("{id:guid}/members/{userId:guid}/role")]
    public async Task<ActionResult<GroupMemberDto>> ChangeRole(Guid id, Guid userId, [FromBody] ChangeRoleRequest request)
    {
        return Ok(await _groups.ChangeRoleAsync(User.GetUserId(), id, userId, request.Role!.Value));
    }

    // PUT /api/groups/{id}/members/me/nickname  { "nickname": "..." } -> 200
    // "me" doesn't match the {userId:guid} routes above, so there's no clash.
    [HttpPut("{id:guid}/members/me/nickname")]
    public async Task<ActionResult<GroupMemberDto>> SetMyNickname(Guid id, [FromBody] SetNicknameRequest request)
    {
        return Ok(await _groups.SetMyNicknameAsync(User.GetUserId(), id, request.Nickname));
    }

    // POST /api/groups/{id}/leave -> 204
    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id)
    {
        await _groups.LeaveAsync(User.GetUserId(), id);
        return NoContent();
    }
}
