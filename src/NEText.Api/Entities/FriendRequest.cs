namespace NEText.Api.Entities;

public class FriendRequest
{
    public Guid Id { get; set; }

    public Guid FromUserId { get; set; }
    public User From { get; set; } = null!;

    public Guid ToUserId { get; set; }
    public User To { get; set; } = null!;

    public FriendRequestStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RespondedAtUtc { get; set; }
}

public enum FriendRequestStatus
{
    Pending,
    Accepted,
    Declined
}
