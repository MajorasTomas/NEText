namespace NEText.Api.Entities;

// One row per pair of users. An Accepted request IS the friendship.
// Becomes the "FriendRequests" table, with two foreign keys to "Users".
public class FriendRequest
{
    public Guid Id { get; set; }

    // Foreign key + navigation property: FromUserId is the column, From is the related User object.
    // EF only fills From/To when you ask for it (Include, or using them inside a query/Select).
    public Guid FromUserId { get; set; }
    public User From { get; set; } = null!;

    public Guid ToUserId { get; set; }
    public User To { get; set; } = null!;

    public FriendRequestStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    // When the recipient accepted or declined. For accepted requests = "friends since".
    public DateTime? RespondedAtUtc { get; set; }
}

public enum FriendRequestStatus
{
    Pending,
    Accepted,
    Declined
}
