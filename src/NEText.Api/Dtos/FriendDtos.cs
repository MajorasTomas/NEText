using System.ComponentModel.DataAnnotations;
using NEText.Api.Entities;

namespace NEText.Api.Dtos;

public record SendFriendRequestRequest(
    [Required] Guid? ToUserId
);

public record FriendRequestDto(
    Guid Id,
    UserDto From,
    UserDto To,
    FriendRequestStatus Status,
    DateTime CreatedAtUtc,
    DateTime? RespondedAtUtc
);

public record FriendDto(Guid UserId, string Username, DateTime FriendsSinceUtc);
