using System.ComponentModel.DataAnnotations;
using NEText.Api.Entities;

namespace NEText.Api.Dtos;

// Body of POST /api/conversations: who to chat with.
public record StartConversationRequest(
    [Required] Guid? UserId
);

// One entry in "my conversations" (direct chats and groups).
public record ConversationDto(
    Guid Id,
    ConversationType Type,     // "Direct" or "Group"
    string? Name,              // groups only
    UserDto? OtherUser,        // direct chats only: the person I'm talking to
    int MemberCount,
    MessageDto? LastMessage,   // null if nothing was sent yet
    int UnreadCount,           // messages from others since I last marked the chat as read
    DateTime CreatedAtUtc
);

// Query string of GET /api/conversations/{id}/messages, e.g. ?limit=20&before=...&unreadOnly=true
// [FromQuery] on a class binds each property from the query parameter with the same name (case-insensitive).
// A class (not a record) so that properties left out of the URL keep these default values.
public class MessageListQuery
{
    // Id of the oldest message you already have: returns messages older than it.
    public Guid? Before { get; set; }

    [Range(1, 100)]
    public int Limit { get; set; } = 50;

    // false = leave out deleted messages (by default they're returned as placeholders).
    public bool IncludeDeleted { get; set; } = true;

    // true = only messages from others sent after I last marked the chat as read (same rule as UnreadCount).
    public bool UnreadOnly { get; set; }
}

// Body of POST /api/conversations/{id}/messages.
public record SendMessageRequest(
    [Required, MinLength(1), MaxLength(4000)] string Text
);

// Body of PUT /api/messages/{id}.
public record EditMessageRequest(
    [Required, MinLength(1), MaxLength(4000)] string Text
);

public record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderUsername,
    string? Text,              // null when deleted
    DateTime SentAtUtc,
    DateTime? EditedAtUtc,
    bool IsDeleted
);
