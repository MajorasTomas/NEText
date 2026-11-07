using NEText.Api.Dtos;
using NEText.Api.Entities;

namespace NEText.Api.Services;

// Managing group chats. Messaging in a group uses the normal conversation endpoints.
// A group id that isn't a group I'm in (or is a direct chat) gives 404. Admin-only actions give 403 to members.
public interface IGroupService
{
    // I become Admin, the others Members. Every member must be my friend.
    Task<ConversationDto> CreateAsync(Guid myId, string name, List<Guid> memberIds);

    // Any member. role = null returns everyone; otherwise only members with that role.
    Task<List<GroupMemberDto>> GetMembersAsync(Guid myId, Guid groupId, ParticipantRole? role);

    // Admin only. The new member must be my friend.
    Task<GroupMemberDto> AddMemberAsync(Guid myId, Guid groupId, Guid userId);

    // Admin only. Not for removing yourself (use LeaveAsync).
    Task RemoveMemberAsync(Guid myId, Guid groupId, Guid userId);

    // Any member. If the last admin leaves, the longest-standing member becomes admin.
    // If the last member leaves, the group is deleted.
    Task LeaveAsync(Guid myId, Guid groupId);

    // Admin only.
    Task<ConversationDto> RenameAsync(Guid myId, Guid groupId, string name);

    // Admin only. A group always keeps at least one admin.
    Task<GroupMemberDto> ChangeRoleAsync(Guid myId, Guid groupId, Guid userId, ParticipantRole role);

    // Any member, for themselves.
    Task<GroupMemberDto> SetMyNicknameAsync(Guid myId, Guid groupId, string? nickname);

    // Admin only. Deletes the group with all its messages.
    Task DeleteAsync(Guid myId, Guid groupId);
}
