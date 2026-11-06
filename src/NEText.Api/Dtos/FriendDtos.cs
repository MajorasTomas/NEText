using System.ComponentModel.DataAnnotations;
using NEText.Api.Entities;

namespace NEText.Api.Dtos;

// Body of POST /api/friend-requests.
// Guid? + [Required]: a missing "toUserId" gives 400 (a plain Guid would silently default to 00000000-...).
public record SendFriendRequestRequest(
    [Required] Guid? ToUserId
);

// A friend request, with both users. Status is sent as text in JSON ("Pending", "Accepted").
public record FriendRequestDto(
    Guid Id,
    UserDto From,
    UserDto To,
    FriendRequestStatus Status,
    DateTime CreatedAtUtc,
    DateTime? RespondedAtUtc
);

// One entry in "my friends": the other user, and since when.
public record FriendDto(Guid UserId, string Username, DateTime FriendsSinceUtc);
