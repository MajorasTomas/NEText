using System.ComponentModel.DataAnnotations;
using NEText.Api.Entities;

namespace NEText.Api.Dtos;

public record StartConversationRequest(
    [Required] Guid? UserId
);

public record ConversationDto(
    Guid Id,
    ConversationType Type,
    string? Name,
    UserDto? OtherUser,
    int MemberCount,
    MessageDto? LastMessage,
    int UnreadCount,
    DateTime CreatedAtUtc
);

public class MessageListQuery
{
    public Guid? Before { get; set; }

    [Range(1, 100)]
    public int Limit { get; set; } = 50;

    public bool IncludeDeleted { get; set; } = true;

    public bool UnreadOnly { get; set; }
}

public record SendMessageRequest(
    [Required, MinLength(1), MaxLength(4000)] string Text
);

public record EditMessageRequest(
    [Required, MinLength(1), MaxLength(4000)] string Text
);

public record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderUsername,
    string? Text,
    DateTime SentAtUtc,
    DateTime? EditedAtUtc,
    bool IsDeleted
);
