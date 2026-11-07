using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NEText.Api.Data;
using NEText.Api.Dtos;
using NEText.Api.Entities;
using NEText.Api.Errors;

namespace NEText.Api.Services;

public class GroupService : IGroupService
{
    private const int MaxMembers = 50;

    private readonly AppDbContext _db;
    private readonly IFriendService _friends;
    private readonly IConversationService _conversations;

    public GroupService(AppDbContext db, IFriendService friends, IConversationService conversations)
    {
        _db = db;
        _friends = friends;
        _conversations = conversations;
    }

    private static readonly Expression<Func<ConversationParticipant, GroupMemberDto>> ToMemberDto = p => new GroupMemberDto(
        new UserDto(p.User.Id, p.User.Username, p.User.CreatedAtUtc),
        p.Role,
        p.Nickname,
        p.JoinedAtUtc);

    public async Task<ConversationDto> CreateAsync(Guid myId, string name, List<Guid> memberIds)
    {
        var groupName = RequireName(name);

        var otherIds = memberIds.Distinct().Where(id => id != myId).ToList();
        if (otherIds.Count == 0)
        {
            throw new BadRequestException("A group needs at least one other member.");
        }
        if (otherIds.Count + 1 > MaxMembers)
        {
            throw new BadRequestException($"A group can have at most {MaxMembers} members.");
        }

        var existingIds = await _db.Users.Where(u => otherIds.Contains(u.Id)).Select(u => u.Id).ToListAsync();
        var unknownId = otherIds.Except(existingIds).FirstOrDefault();
        if (unknownId != Guid.Empty)
        {
            throw new NotFoundException($"User '{unknownId}' was not found.");
        }

        var friendIds = await _friends.GetFriendIdsAsync(myId);
        if (otherIds.Any(id => !friendIds.Contains(id)))
        {
            throw new ForbiddenException("You can only add your friends to a group.");
        }

        var now = DateTime.UtcNow;
        var group = new Conversation
        {
            Id = Guid.CreateVersion7(),
            Type = ConversationType.Group,
            Name = groupName,
            CreatedAtUtc = now
        };
        group.Participants.Add(new ConversationParticipant { UserId = myId, Role = ParticipantRole.Admin, JoinedAtUtc = now });
        foreach (var id in otherIds)
        {
            group.Participants.Add(new ConversationParticipant { UserId = id, Role = ParticipantRole.Member, JoinedAtUtc = now });
        }

        _db.Conversations.Add(group);
        await _db.SaveChangesAsync();

        return await _conversations.GetAsync(myId, group.Id);
    }

    public async Task<List<GroupMemberDto>> GetMembersAsync(Guid myId, Guid groupId, ParticipantRole? role)
    {
        await FindMyMembershipAsync(myId, groupId);

        if (role is not null && !Enum.IsDefined(role.Value))
        {
            throw new BadRequestException("Unknown role. Use 'Admin' or 'Member'.");
        }

        var query = _db.ConversationParticipants.Where(p => p.ConversationId == groupId);

        if (role is ParticipantRole wanted)
        {
            query = query.Where(p => p.Role == wanted);
        }

        return await query
            .OrderByDescending(p => p.Role == ParticipantRole.Admin)
            .ThenBy(p => p.User.Username)
            .Select(ToMemberDto)
            .ToListAsync();
    }

    public async Task<GroupMemberDto> AddMemberAsync(Guid myId, Guid groupId, Guid userId)
    {
        await RequireAdminAsync(myId, groupId);

        if (!await _db.Users.AnyAsync(u => u.Id == userId))
        {
            throw new NotFoundException($"User '{userId}' was not found.");
        }
        if (await _db.ConversationParticipants.AnyAsync(p => p.ConversationId == groupId && p.UserId == userId))
        {
            throw new ConflictException("This user is already a member of the group.");
        }
        if (!await _friends.AreFriendsAsync(myId, userId))
        {
            throw new ForbiddenException("You can only add your friends to a group.");
        }
        if (await _db.ConversationParticipants.CountAsync(p => p.ConversationId == groupId) >= MaxMembers)
        {
            throw new ConflictException($"The group is full ({MaxMembers} members).");
        }

        var now = DateTime.UtcNow;
        _db.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationId = groupId,
            UserId = userId,
            Role = ParticipantRole.Member,
            JoinedAtUtc = now,
            LastReadAtUtc = now
        });
        await _db.SaveChangesAsync();

        return await GetMemberDtoAsync(groupId, userId);
    }

    public async Task RemoveMemberAsync(Guid myId, Guid groupId, Guid userId)
    {
        await RequireAdminAsync(myId, groupId);

        if (userId == myId)
        {
            throw new BadRequestException("To leave the group yourself, use leave.");
        }

        var member = await _db.ConversationParticipants
                         .FirstOrDefaultAsync(p => p.ConversationId == groupId && p.UserId == userId)
                     ?? throw new NotFoundException("This user is not a member of the group.");

        _db.ConversationParticipants.Remove(member);
        await _db.SaveChangesAsync();
    }

    public async Task LeaveAsync(Guid myId, Guid groupId)
    {
        var me = await FindMyMembershipAsync(myId, groupId);
        _db.ConversationParticipants.Remove(me);

        var others = await _db.ConversationParticipants
            .Where(p => p.ConversationId == groupId && p.UserId != myId)
            .OrderBy(p => p.JoinedAtUtc)
            .ThenBy(p => p.UserId)
            .ToListAsync();

        if (others.Count == 0)
        {
            var group = await _db.Conversations.SingleAsync(c => c.Id == groupId);
            _db.Conversations.Remove(group);
        }
        else if (me.Role == ParticipantRole.Admin && others.All(p => p.Role != ParticipantRole.Admin))
        {
            others[0].Role = ParticipantRole.Admin;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<ConversationDto> RenameAsync(Guid myId, Guid groupId, string name)
    {
        var me = await RequireAdminAsync(myId, groupId);

        me.Conversation.Name = RequireName(name);
        await _db.SaveChangesAsync();

        return await _conversations.GetAsync(myId, groupId);
    }

    public async Task<GroupMemberDto> ChangeRoleAsync(Guid myId, Guid groupId, Guid userId, ParticipantRole role)
    {
        await RequireAdminAsync(myId, groupId);

        var member = await _db.ConversationParticipants
                         .FirstOrDefaultAsync(p => p.ConversationId == groupId && p.UserId == userId)
                     ?? throw new NotFoundException("This user is not a member of the group.");

        if (member.Role == ParticipantRole.Admin && role == ParticipantRole.Member)
        {
            var adminCount = await _db.ConversationParticipants
                .CountAsync(p => p.ConversationId == groupId && p.Role == ParticipantRole.Admin);
            if (adminCount == 1)
            {
                throw new ConflictException("A group needs at least one admin.");
            }
        }

        member.Role = role;
        await _db.SaveChangesAsync();

        return await GetMemberDtoAsync(groupId, userId);
    }

    public async Task<GroupMemberDto> SetMyNicknameAsync(Guid myId, Guid groupId, string? nickname)
    {
        var me = await FindMyMembershipAsync(myId, groupId);

        me.Nickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim();
        await _db.SaveChangesAsync();

        return await GetMemberDtoAsync(groupId, myId);
    }

    public async Task DeleteAsync(Guid myId, Guid groupId)
    {
        var me = await RequireAdminAsync(myId, groupId);

        _db.Conversations.Remove(me.Conversation);
        await _db.SaveChangesAsync();
    }

    private async Task<ConversationParticipant> FindMyMembershipAsync(Guid myId, Guid groupId)
    {
        return await _db.ConversationParticipants
                   .Include(p => p.Conversation)
                   .FirstOrDefaultAsync(p => p.ConversationId == groupId
                                             && p.UserId == myId
                                             && p.Conversation.Type == ConversationType.Group)
               ?? throw new NotFoundException("Group not found.");
    }

    private async Task<ConversationParticipant> RequireAdminAsync(Guid myId, Guid groupId)
    {
        var me = await FindMyMembershipAsync(myId, groupId);
        if (me.Role != ParticipantRole.Admin)
        {
            throw new ForbiddenException("Only group admins can do this.");
        }
        return me;
    }

    private Task<GroupMemberDto> GetMemberDtoAsync(Guid groupId, Guid userId)
    {
        return _db.ConversationParticipants
            .Where(p => p.ConversationId == groupId && p.UserId == userId)
            .Select(ToMemberDto)
            .SingleAsync();
    }

    private static string RequireName(string name)
    {
        var trimmed = name.Trim();
        return trimmed.Length > 0 ? trimmed : throw new BadRequestException("Group name can't be empty.");
    }
}
