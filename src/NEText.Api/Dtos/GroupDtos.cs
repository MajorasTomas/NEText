using System.ComponentModel.DataAnnotations;
using NEText.Api.Entities;

namespace NEText.Api.Dtos;

// Body of POST /api/groups.
public record CreateGroupRequest(
    [Required, MinLength(1), MaxLength(64)] string Name,

    // The other members (I'm added automatically, as admin). Must be my friends.
    [Required, MinLength(1)] List<Guid> MemberIds
);

// Body of PATCH /api/groups/{id}.
public record RenameGroupRequest(
    [Required, MinLength(1), MaxLength(64)] string Name
);

// Body of POST /api/groups/{id}/members.
public record AddGroupMemberRequest(
    [Required] Guid? UserId
);

// Body of PUT /api/groups/{id}/members/{userId}/role.
public record ChangeRoleRequest(
    [Required] ParticipantRole? Role
);

// Body of PUT /api/groups/{id}/members/me/nickname. Null or empty removes the nickname.
public record SetNicknameRequest(
    [MaxLength(32)] string? Nickname
);

public record GroupMemberDto(UserDto User, ParticipantRole Role, string? Nickname, DateTime JoinedAtUtc);
