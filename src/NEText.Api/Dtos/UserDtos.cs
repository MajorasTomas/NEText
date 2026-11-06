namespace NEText.Api.Dtos;

// Public view of a user: what other users are allowed to see. No email, no password hash.
public record UserDto(Guid Id, string Username, DateTime CreatedAtUtc);

// The logged-in user's view of themselves: includes their own email.
public record CurrentUserDto(Guid Id, string Username, string Email, DateTime CreatedAtUtc);
