using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NEText.Api.Data;
using NEText.Api.Dtos;
using NEText.Api.Entities;
using NEText.Api.Errors;
using Npgsql;

namespace NEText.Api.Services;

public class FriendService : IFriendService
{
    private readonly AppDbContext _db;

    public FriendService(AppDbContext db)
    {
        _db = db;
    }

    // Entity -> DTO as an expression tree, so EF can translate it into the SELECT (joining both users)
    // instead of loading whole entities. Reused by every query that returns FriendRequestDto.
    private static readonly Expression<Func<FriendRequest, FriendRequestDto>> ToDto = r => new FriendRequestDto(
        r.Id,
        new UserDto(r.From.Id, r.From.Username, r.From.CreatedAtUtc),
        new UserDto(r.To.Id, r.To.Username, r.To.CreatedAtUtc),
        r.Status,
        r.CreatedAtUtc,
        r.RespondedAtUtc);

    public async Task<FriendRequestDto> SendRequestAsync(Guid myId, Guid toUserId)
    {
        if (toUserId == myId)
        {
            throw new BadRequestException("You can't send a friend request to yourself.");
        }
        if (!await _db.Users.AnyAsync(u => u.Id == toUserId))
        {
            throw new NotFoundException($"User '{toUserId}' was not found.");
        }

        var request = await FindBetweenAsync(myId, toUserId);

        if (request is null)
        {
            request = new FriendRequest
            {
                Id = Guid.CreateVersion7(),
                FromUserId = myId,
                ToUserId = toUserId,
                Status = FriendRequestStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.FriendRequests.Add(request);
        }
        else
        {
            switch (request.Status)
            {
                case FriendRequestStatus.Accepted:
                    throw new ConflictException("You are already friends.");

                case FriendRequestStatus.Pending when request.FromUserId == myId:
                    throw new ConflictException("Friend request already sent.");

                case FriendRequestStatus.Pending:
                    throw new ConflictException("This user already sent you a friend request. Accept it instead.");

                case FriendRequestStatus.Declined:
                    // Reuse the declined row (in either direction): it becomes a new pending request from me.
                    request.FromUserId = myId;
                    request.ToUserId = toUserId;
                    request.Status = FriendRequestStatus.Pending;
                    request.CreatedAtUtc = DateTime.UtcNow;
                    request.RespondedAtUtc = null;
                    break;
            }
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Safety net for two identical requests arriving at the same moment.
            throw new ConflictException("Friend request already sent.");
        }

        return await GetDtoAsync(request.Id);
    }

    public async Task<List<FriendRequestDto>> GetIncomingAsync(Guid myId)
    {
        return await _db.FriendRequests
            .Where(r => r.ToUserId == myId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(ToDto)
            .ToListAsync();
    }

    public async Task<List<FriendRequestDto>> GetOutgoingAsync(Guid myId)
    {
        return await _db.FriendRequests
            .Where(r => r.FromUserId == myId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(ToDto)
            .ToListAsync();
    }

    public async Task<FriendRequestDto> AcceptAsync(Guid myId, Guid requestId)
    {
        var request = await FindPendingForRecipientAsync(myId, requestId);

        request.Status = FriendRequestStatus.Accepted;
        request.RespondedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetDtoAsync(request.Id);
    }

    public async Task DeclineAsync(Guid myId, Guid requestId)
    {
        var request = await FindPendingForRecipientAsync(myId, requestId);

        // Kept in the database (not deleted). The sender no longer sees it and may send again.
        request.Status = FriendRequestStatus.Declined;
        request.RespondedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task CancelAsync(Guid myId, Guid requestId)
    {
        // Only my own, still pending request. Anything else looks like "doesn't exist" to me.
        var request = await _db.FriendRequests.FirstOrDefaultAsync(r =>
                r.Id == requestId && r.FromUserId == myId && r.Status == FriendRequestStatus.Pending)
            ?? throw new NotFoundException("Friend request not found.");

        _db.FriendRequests.Remove(request);
        await _db.SaveChangesAsync();
    }

    public async Task<List<FriendDto>> GetFriendsAsync(Guid myId)
    {
        // Friendship is symmetric: I can be on either side of the accepted request.
        // Load both sides, then pick "the other one" in C#.
        var rows = await _db.FriendRequests
            .Where(r => r.Status == FriendRequestStatus.Accepted && (r.FromUserId == myId || r.ToUserId == myId))
            .Select(r => new
            {
                r.FromUserId,
                FromUsername = r.From.Username,
                r.ToUserId,
                ToUsername = r.To.Username,
                Since = r.RespondedAtUtc ?? r.CreatedAtUtc
            })
            .ToListAsync();

        return rows
            .Select(r => r.FromUserId == myId
                ? new FriendDto(r.ToUserId, r.ToUsername, r.Since)
                : new FriendDto(r.FromUserId, r.FromUsername, r.Since))
            .OrderBy(f => f.Username)
            .ToList();
    }

    public async Task RemoveFriendAsync(Guid myId, Guid friendUserId)
    {
        var request = await FindBetweenAsync(myId, friendUserId);
        if (request is null || request.Status != FriendRequestStatus.Accepted)
        {
            throw new NotFoundException("You are not friends with this user.");
        }

        _db.FriendRequests.Remove(request);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> AreFriendsAsync(Guid userA, Guid userB)
    {
        var request = await FindBetweenAsync(userA, userB);
        return request?.Status == FriendRequestStatus.Accepted;
    }

    public async Task<HashSet<Guid>> GetFriendIdsAsync(Guid myId)
    {
        var ids = await _db.FriendRequests
            .Where(r => r.Status == FriendRequestStatus.Accepted && (r.FromUserId == myId || r.ToUserId == myId))
            .Select(r => r.FromUserId == myId ? r.ToUserId : r.FromUserId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    // The single row between two users, whichever of them sent it.
    private Task<FriendRequest?> FindBetweenAsync(Guid userA, Guid userB)
    {
        return _db.FriendRequests.FirstOrDefaultAsync(r =>
            (r.FromUserId == userA && r.ToUserId == userB) ||
            (r.FromUserId == userB && r.ToUserId == userA));
    }

    // A request sent to me. 404 if it doesn't exist or isn't mine (don't reveal other people's requests),
    // 409 if it was already accepted/declined.
    private async Task<FriendRequest> FindPendingForRecipientAsync(Guid myId, Guid requestId)
    {
        var request = await _db.FriendRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.ToUserId == myId)
            ?? throw new NotFoundException("Friend request not found.");

        if (request.Status != FriendRequestStatus.Pending)
        {
            throw new ConflictException("This friend request is no longer pending.");
        }

        return request;
    }

    private Task<FriendRequestDto> GetDtoAsync(Guid requestId)
    {
        return _db.FriendRequests.Where(r => r.Id == requestId).Select(ToDto).SingleAsync();
    }
}
