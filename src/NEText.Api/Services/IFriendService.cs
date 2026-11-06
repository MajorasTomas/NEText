using NEText.Api.Dtos;

namespace NEText.Api.Services;

// All methods take "myId" = the logged-in user (from the token), so every rule is checked from their point of view.
public interface IFriendService
{
    // 400 to yourself, 404 unknown user, 409 already friends / already pending (either direction).
    // A previously declined request is reused and becomes pending again.
    Task<FriendRequestDto> SendRequestAsync(Guid myId, Guid toUserId);

    // Pending requests sent to me.
    Task<List<FriendRequestDto>> GetIncomingAsync(Guid myId);

    // Pending requests I sent (declined ones are not shown: to the sender it looks like they can send again).
    Task<List<FriendRequestDto>> GetOutgoingAsync(Guid myId);

    // Only the recipient. 404 if it's not their request, 409 if it's no longer pending.
    Task<FriendRequestDto> AcceptAsync(Guid myId, Guid requestId);
    Task DeclineAsync(Guid myId, Guid requestId);

    // Only the sender, only while pending. Deletes the request.
    Task CancelAsync(Guid myId, Guid requestId);

    Task<List<FriendDto>> GetFriendsAsync(Guid myId);

    // Deletes the friendship (either of the two can do it). 404 if not friends.
    Task RemoveFriendAsync(Guid myId, Guid friendUserId);
}
