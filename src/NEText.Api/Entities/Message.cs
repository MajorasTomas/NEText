namespace NEText.Api.Entities;

public class Message
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;

    // Cleared (empty) when the message is deleted; the row itself stays (soft delete).
    public string Text { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; }
    public DateTime? EditedAtUtc { get; set; }

    // Soft delete: set instead of removing the row, so the chat can show "message deleted".
    public DateTime? DeletedAtUtc { get; set; }
}
