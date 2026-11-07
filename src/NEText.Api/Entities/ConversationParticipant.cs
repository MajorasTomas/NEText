namespace NEText.Api.Entities;

// Join table between Users and Conversations (many-to-many: a user is in many conversations,
// a conversation has many users). One row = "this user is in this conversation".
// Primary key is the pair (ConversationId, UserId), see AppDbContext. Later: Role, Nickname, ...
public class ConversationParticipant
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime JoinedAtUtc { get; set; }

    // Messages sent after this moment (by others) are unread for this user.
    public DateTime? LastReadAtUtc { get; set; }
}
