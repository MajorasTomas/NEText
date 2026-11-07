namespace NEText.Api.Entities;

public class Conversation
{
    public Guid Id { get; set; }

    public ConversationType Type { get; set; }

    public string? Name { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastMessageAtUtc { get; set; }

    public List<ConversationParticipant> Participants { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}

public enum ConversationType
{
    Direct,
    Group
}
