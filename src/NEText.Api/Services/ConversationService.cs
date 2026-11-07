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

    // A service can depend on another service: DI builds FriendService (with its own AppDbContext,
    // the same scoped instance as ours) and passes it in.
    public ConversationService(AppDbContext db, IFriendService friends)
    {
        _db = db;
        _friends = friends;
    }

    // Message -> DTO, translated by EF into the SELECT. Deleted messages hide their text.
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

        // Already have a 1:1 chat with this user? Return it (even if we're no longer friends: history stays).
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

        // Adding the conversation also adds the participants inside it: EF saves the whole object graph
        // (1 INSERT into Conversations + 2 INSERTs into ConversationParticipants, in one transaction).
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        return (await GetAsync(myId, conversation.Id), true);
    }

    public async Task<List<ConversationDto>> GetMyConversationsAsync(Guid myId)
    {
        return await QueryConversations(myId).ToListAsync();
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

    public async Task<List<MessageDto>> GetMessagesAsync(Guid myId, Guid conversationId, Guid? before, int limit, bool includeDeleted)
    {
        await FindMyParticipationAsync(myId, conversationId); // 404 if it's not my conversation

        var query = _db.Messages.Where(m => m.ConversationId == conversationId);

        // Optional filter: leave out "message deleted" placeholders.
        if (!includeDeleted)
        {
            query = query.Where(m => m.DeletedAtUtc == null);
        }

        // Cursor pagination: "messages older than this one". Unlike page numbers, new messages arriving
        // in the meantime don't shift what the next page contains.
        if (before is Guid beforeId)
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
            .Take(limit)
            .Select(ToMessageDto)
            .ToListAsync();
    }

    public async Task<MessageDto> SendMessageAsync(Guid myId, Guid conversationId, string text)
    {
        var me = await FindMyParticipationAsync(myId, conversationId);
        var trimmed = RequireText(text);
        var conversation = await _db.Conversations.SingleAsync(c => c.Id == conversationId);

        // Direct chat rule: the other person must still be my friend.
        // Groups have no such rule: being a participant is enough.
        if (conversation.Type == ConversationType.Direct)
        {
            var otherUserId = await _db.ConversationParticipants
                .Where(p => p.ConversationId == conversationId && p.UserId != myId)
                .Select(p => p.UserId)
                .SingleAsync(); // a direct chat has exactly one other participant

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

        // All three changes are saved together by one SaveChanges (one transaction).
        conversation.LastMessageAtUtc = now;   // for sorting "my conversations"
        me.LastReadAtUtc = now;                 // my own message doesn't make the chat unread for me

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
            return; // already deleted: DELETE is idempotent, the result is the same
        }

        // Soft delete: keep the row (the chat shows "message deleted"), but don't keep the content.
        message.Text = string.Empty;
        message.DeletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // The ConversationDto query, seen from my side: the other user, the last message and my unread count
    // are all computed by the database in one SQL query (with subqueries).
    private IQueryable<ConversationDto> QueryConversations(Guid myId, Guid? conversationId = null)
    {
        var mine = _db.ConversationParticipants.Where(p => p.UserId == myId);
        if (conversationId is Guid id)
        {
            mine = mine.Where(p => p.ConversationId == id);
        }

        return mine
            .OrderByDescending(p => p.Conversation.LastMessageAtUtc ?? p.Conversation.CreatedAtUtc)
            .Select(p => new ConversationDto(
                p.ConversationId,
                p.Conversation.Participants
                    .Where(other => other.UserId != myId)
                    .Select(other => new UserDto(other.User.Id, other.User.Username, other.User.CreatedAtUtc))
                    .FirstOrDefault()!,
                p.Conversation.Messages
                    .AsQueryable()            // lets us reuse the ToMessageDto expression inside this query
                    .OrderByDescending(m => m.SentAtUtc)
                    .Select(ToMessageDto)
                    .FirstOrDefault(),
                p.Conversation.Messages.Count(m =>
                    m.SenderId != myId
                    && m.DeletedAtUtc == null
                    && (p.LastReadAtUtc == null || m.SentAtUtc > p.LastReadAtUtc)),
                p.Conversation.CreatedAtUtc));
    }

    // My participant row (tracked, so it can be changed). 404 if it's not my conversation.
    private async Task<ConversationParticipant> FindMyParticipationAsync(Guid myId, Guid conversationId)
    {
        return await _db.ConversationParticipants
                   .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == myId)
               ?? throw new NotFoundException("Conversation not found.");
    }

    // A message in one of my conversations (tracked). 404 otherwise.
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

    // [MinLength(1)] still allows "   ", so check after trimming.
    private static string RequireText(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length > 0 ? trimmed : throw new BadRequestException("Message text can't be empty.");
    }
}
