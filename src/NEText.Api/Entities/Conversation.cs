namespace NEText.Api.Entities;

// A chat: either a 1:1 (Direct) or a Group. The type is stored, not guessed from the participant count,
// because a group can shrink to 2 people and is still a group with group rules.
public class Conversation
{
    public Guid Id { get; set; }

    public ConversationType Type { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    // Updated on every new message, so "my conversations" can be sorted by latest activity cheaply.
    public DateTime? LastMessageAtUtc { get; set; }

    // Collection navigation properties: "one conversation has many participants / messages".
    // EF fills them only when used in a query (Include, Where(c => c.Participants.Any(...)), Select...).
    public List<ConversationParticipant> Participants { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}

public enum ConversationType
{
    Direct, // exactly two people, who must be friends to message each other
    Group   // not built yet; membership alone allows messaging
}
