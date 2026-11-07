namespace NEText.Api.Entities;

public class Message
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public string Text { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; }
    public DateTime? EditedAtUtc { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
