using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NEText.Api.Data;
using NEText.Api.Dtos;
using NEText.Api.Entities;
using NEText.Api.Errors;

namespace NEText.Api.Services;

public class ConversationService : IConversationService
{
    private readonly AppDbContext _db;
    private readonly IFriendService _friends;

    public ConversationService(AppDbContext db, IFriendService friends)
    {
        _db = db;
        _friends = friends;
    }

    private static readonly Expression<Func<Message, MessageDto>> ToMessageDto = m => new MessageDto(
        m.Id,
        m.ConversationId,
        m.SenderId,
        m.Sender.Username,
        m.DeletedAtUtc == null ? m.Text : null,
        m.SentAtUtc,
        m.EditedAtUtc,
        m.DeletedAtUtc != null);

    public async Task<(ConversationDto Conversation, bool Created)> StartAsync(Guid myId, Guid otherUserId)
    {
        if (otherUserId == myId)
        {
            throw new BadRequestException("You can't start a conversation with yourself.");
        }
        if (!await _db.Users.AnyAsync(u => u.Id == otherUserId))
        {
            throw new NotFoundException($"User '{otherUserId}' was not found.");
        }

        var existingId = await _db.Conversations
            .Where(c => c.Type == ConversationType.Direct
                        && c.Participants.Any(p => p.UserId == myId)
                        && c.Participants.Any(p => p.UserId == otherUserId))
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();

        if (existingId is Guid id)
        {
            return (await GetAsync(myId, id), false);
        }

        if (!await _friends.AreFriendsAsync(myId, otherUserId))
        {
            throw new ForbiddenException("You can only start conversations with friends.");
        }

        var now = DateTime.UtcNow;
        var conversation = new Conversation
        {
            Id = Guid.CreateVersion7(),
            Type = ConversationType.Direct,
            CreatedAtUtc = now,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = myId, JoinedAtUtc = now },
                new() { UserId = otherUserId, JoinedAtUtc = now }
            }
        };

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        return (await GetAsync(myId, conversation.Id), true);
    }

    public async Task<List<ConversationDto>> GetMyConversationsAsync(Guid myId, ConversationType? type)
    {
        return await QueryConversations(myId, type: type).ToListAsync();
    }

    public async Task<ConversationDto> GetAsync(Guid myId, Guid conversationId)
    {
        return await QueryConversations(myId, conversationId).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Conversation not found.");
    }

    public async Task MarkAsReadAsync(Guid myId, Guid conversationId)
    {
        var me = await FindMyParticipationAsync(myId, conversationId);
        me.LastReadAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<List<MessageDto>> GetMessagesAsync(Guid myId, Guid conversationId, MessageListQuery options)
    {
        var me = await FindMyParticipationAsync(myId, conversationId);

        var query = _db.Messages.Where(m => m.ConversationId == conversationId);

        if (!options.IncludeDeleted)
        {
            query = query.Where(m => m.DeletedAtUtc == null);
        }

        if (options.UnreadOnly)
        {
            query = query.Where(m => m.SenderId != myId && m.DeletedAtUtc == null);
            if (me.LastReadAtUtc is DateTime lastRead)
            {
                query = query.Where(m => m.SentAtUtc > lastRead);
            }
        }

        if (options.Before is Guid beforeId)
        {
            var cursor = await _db.Messages
                .Where(m => m.Id == beforeId && m.ConversationId == conversationId)
                .Select(m => (DateTime?)m.SentAtUtc)
                .FirstOrDefaultAsync()
                ?? throw new BadRequestException("'before' must be the id of a message in this conversation.");

            query = query.Where(m => m.SentAtUtc < cursor);
        }

        return await query
            .OrderByDescending(m => m.SentAtUtc)
            .Take(options.Limit)
            .Select(ToMessageDto)
            .ToListAsync();
    }

    public async Task<MessageDto> SendMessageAsync(Guid myId, Guid conversationId, string text)
    {
        var me = await FindMyParticipationAsync(myId, conversationId);
        var trimmed = RequireText(text);
        var conversation = await _db.Conversations.SingleAsync(c => c.Id == conversationId);

        if (conversation.Type == ConversationType.Direct)
        {
            var otherUserId = await _db.ConversationParticipants
                .Where(p => p.ConversationId == conversationId && p.UserId != myId)
                .Select(p => p.UserId)
                .SingleAsync();

            if (!await _friends.AreFriendsAsync(myId, otherUserId))
            {
                throw new ForbiddenException("You are no longer friends with this user, so you can't send new messages.");
            }
        }

        var now = DateTime.UtcNow;
        var message = new Message
        {
            Id = Guid.CreateVersion7(),
            ConversationId = conversationId,
            SenderId = myId,
            Text = trimmed,
            SentAtUtc = now
        };
        _db.Messages.Add(message);

        conversation.LastMessageAtUtc = now;
        me.LastReadAtUtc = now;

        await _db.SaveChangesAsync();
        return await GetMessageDtoAsync(message.Id);
    }

    public async Task<MessageDto> EditMessageAsync(Guid myId, Guid messageId, string text)
    {
        var message = await FindMessageAsync(myId, messageId);

        if (message.SenderId != myId)
        {
            throw new ForbiddenException("You can only edit your own messages.");
        }
        if (message.DeletedAtUtc is not null)
        {
            throw new ConflictException("This message was deleted.");
        }

        message.Text = RequireText(text);
        message.EditedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetMessageDtoAsync(message.Id);
    }

    public async Task DeleteMessageAsync(Guid myId, Guid messageId)
    {
        var message = await FindMessageAsync(myId, messageId);

        if (message.SenderId != myId)
        {
            throw new ForbiddenException("You can only delete your own messages.");
        }
        if (message.DeletedAtUtc is not null)
        {
            return;
        }

        message.Text = string.Empty;
        message.DeletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private IQueryable<ConversationDto> QueryConversations(Guid myId, Guid? conversationId = null, ConversationType? type = null)
    {
        var mine = _db.ConversationParticipants.Where(p => p.UserId == myId);
        if (conversationId is Guid id)
        {
            mine = mine.Where(p => p.ConversationId == id);
        }
        if (type is ConversationType wanted)
        {
            mine = mine.Where(p => p.Conversation.Type == wanted);
        }

        return mine
            .OrderByDescending(p => p.Conversation.LastMessageAtUtc ?? p.Conversation.CreatedAtUtc)
            .Select(p => new ConversationDto(
                p.ConversationId,
                p.Conversation.Type,
                p.Conversation.Name,
                p.Conversation.Type == ConversationType.Direct
                    ? p.Conversation.Participants
                        .Where(other => other.UserId != myId)
                        .Select(other => new UserDto(other.User.Id, other.User.Username, other.User.CreatedAtUtc))
                        .FirstOrDefault()
                    : null,
                p.Conversation.Participants.Count,
                p.Conversation.Messages
                    .AsQueryable()
                    .OrderByDescending(m => m.SentAtUtc)
                    .Select(ToMessageDto)
                    .FirstOrDefault(),
                p.Conversation.Messages.Count(m =>
                    m.SenderId != myId
                    && m.DeletedAtUtc == null
                    && (p.LastReadAtUtc == null || m.SentAtUtc > p.LastReadAtUtc)),
                p.Conversation.CreatedAtUtc));
    }

    private async Task<ConversationParticipant> FindMyParticipationAsync(Guid myId, Guid conversationId)
    {
        return await _db.ConversationParticipants
                   .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == myId)
               ?? throw new NotFoundException("Conversation not found.");
    }

    private async Task<Message> FindMessageAsync(Guid myId, Guid messageId)
    {
        return await _db.Messages
                   .FirstOrDefaultAsync(m => m.Id == messageId && m.Conversation.Participants.Any(p => p.UserId == myId))
               ?? throw new NotFoundException("Message not found.");
    }

    private Task<MessageDto> GetMessageDtoAsync(Guid messageId)
    {
        return _db.Messages.Where(m => m.Id == messageId).Select(ToMessageDto).SingleAsync();
    }

    private static string RequireText(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length > 0 ? trimmed : throw new BadRequestException("Message text can't be empty.");
    }
}
