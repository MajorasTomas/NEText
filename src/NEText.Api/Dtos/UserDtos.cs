namespace NEText.Api.Dtos;

// Public view of a user: what other users are allowed to see. No email, no password hash.
public record UserDto(Guid Id, string Username, string DisplayName, DateTime CreatedAtUtc);
