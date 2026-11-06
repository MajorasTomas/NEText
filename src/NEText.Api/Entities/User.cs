namespace NEText.Api.Entities;

// A registered user. Becomes the "Users" table.
public class User
{
    public Guid Id { get; set; }

    // Always stored lowercase, so "Alice" and "alice" can't both exist. Unique (see AppDbContext).
    public string Username { get; set; } = string.Empty;

    // Always stored lowercase. Used to log in. Unique (see AppDbContext). Never shown to other users.
    public string Email { get; set; } = string.Empty;

    // The name shown in the app, keeps the user's preferred spelling, e.g. "Alice".
    public string DisplayName { get; set; } = string.Empty;

    // Salted hash produced by PasswordHasher. The plain password is never stored.
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
