using System.ComponentModel.DataAnnotations;
using NEText.Api.Entities;

namespace NEText.Api.Dtos;

public record CreateGroupRequest(
    [Required, MinLength(1), MaxLength(64)] string Name,

    [Required, MinLength(1)] List<Guid> MemberIds
);

public record RenameGroupRequest(
    [Required, MinLength(1), MaxLength(64)] string Name
);

public record AddGroupMemberRequest(
    [Required] Guid? UserId
);

public record ChangeRoleRequest(
    [Required] ParticipantRole? Role
);

public record SetNicknameRequest(
    [MaxLength(32)] string? Nickname
);

public record GroupMemberDto(UserDto User, ParticipantRole Role, string? Nickname, DateTime JoinedAtUtc);
