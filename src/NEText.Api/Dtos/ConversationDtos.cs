using System.ComponentModel.DataAnnotations;

namespace NEText.Api.Dtos;

// Body of POST /api/conversations: who to chat with.
public record StartConversationRequest(
    [Required] Guid? UserId
);

// One entry in "my conversations".
public record ConversationDto(
    Guid Id,
    UserDto OtherUser,
    MessageDto? LastMessage,   // null if nothing was sent yet
    int UnreadCount,           // messages from the other user since I last marked the chat as read
    DateTime CreatedAtUtc
);

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
