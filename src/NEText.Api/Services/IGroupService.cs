using NEText.Api.Dtos;
using NEText.Api.Entities;

namespace NEText.Api.Services;

public interface IGroupService
{
    Task<ConversationDto> CreateAsync(Guid myId, string name, List<Guid> memberIds);

    Task<List<GroupMemberDto>> GetMembersAsync(Guid myId, Guid groupId, ParticipantRole? role);

    Task<GroupMemberDto> AddMemberAsync(Guid myId, Guid groupId, Guid userId);

    Task RemoveMemberAsync(Guid myId, Guid groupId, Guid userId);

    Task LeaveAsync(Guid myId, Guid groupId);

    Task<ConversationDto> RenameAsync(Guid myId, Guid groupId, string name);

    Task<GroupMemberDto> ChangeRoleAsync(Guid myId, Guid groupId, Guid userId, ParticipantRole role);

    Task<GroupMemberDto> SetMyNicknameAsync(Guid myId, Guid groupId, string? nickname);

    Task DeleteAsync(Guid myId, Guid groupId);
}
