namespace NEText.Api.Entities;

public class ConversationParticipant
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ParticipantRole Role { get; set; }

    public string? Nickname { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    public DateTime? LastReadAtUtc { get; set; }
}

public enum ParticipantRole
{
    Member,
    Admin
}
