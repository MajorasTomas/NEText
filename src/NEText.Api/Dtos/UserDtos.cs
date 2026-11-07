namespace NEText.Api.Dtos;

public record UserDto(Guid Id, string Username, DateTime CreatedAtUtc);

public record CurrentUserDto(Guid Id, string Username, string Email, DateTime CreatedAtUtc);
