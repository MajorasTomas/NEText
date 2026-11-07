using NEText.Api.Dtos;

namespace NEText.Api.Services;

public interface IFriendService
{
    Task<FriendRequestDto> SendRequestAsync(Guid myId, Guid toUserId);

    Task<List<FriendRequestDto>> GetIncomingAsync(Guid myId);

    Task<List<FriendRequestDto>> GetOutgoingAsync(Guid myId);

    Task<FriendRequestDto> AcceptAsync(Guid myId, Guid requestId);
    Task DeclineAsync(Guid myId, Guid requestId);

    Task CancelAsync(Guid myId, Guid requestId);

    Task<List<FriendDto>> GetFriendsAsync(Guid myId);

    Task RemoveFriendAsync(Guid myId, Guid friendUserId);

    Task<bool> AreFriendsAsync(Guid userA, Guid userB);

    Task<HashSet<Guid>> GetFriendIdsAsync(Guid myId);
}
