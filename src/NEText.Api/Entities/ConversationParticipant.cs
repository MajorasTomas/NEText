namespace NEText.Api.Entities;

// Join table between Users and Conversations (many-to-many: a user is in many conversations,
// a conversation has many users). One row = "this user is in this conversation".
// Primary key is the pair (ConversationId, UserId), see AppDbContext.
public class ConversationParticipant
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Only meaningful in groups (in direct chats both are Members).
    public ParticipantRole Role { get; set; }

    // Group-specific name for this member, e.g. "Mom". Null = use the username.
    public string? Nickname { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    // Messages sent after this moment (by others) are unread for this user.
    public DateTime? LastReadAtUtc { get; set; }
}

public enum ParticipantRole
{
    Member, // can read and send messages, set their own nickname, leave
    Admin   // can also add/remove members, rename, change roles, delete the group
}
