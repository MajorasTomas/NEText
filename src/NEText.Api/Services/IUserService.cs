using NEText.Api.Dtos;

namespace NEText.Api.Services;

public interface IUserService
{
    // Throws ConflictException if the username or email is already taken.
    Task<UserDto> RegisterAsync(RegisterRequest request);

    // Throws NotFoundException if there is no such user.
    Task<UserDto> GetByIdAsync(Guid id);

    // Case-insensitive match on username or display name. Returns at most 20 users.
    Task<List<UserDto>> SearchAsync(string query);
}
