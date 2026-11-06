using NEText.Api.Dtos;

namespace NEText.Api.Services;

public interface IUserService
{
    // Throws ConflictException if the username or email is already taken.
    Task<UserDto> RegisterAsync(RegisterRequest request);

    // Checks email + password and returns a JWT. Throws UnauthorizedException if they don't match.
    Task<LoginResponse> LoginAsync(LoginRequest request);

    // The logged-in user (id taken from the token). Throws UnauthorizedException if the user no longer exists.
    Task<CurrentUserDto> GetCurrentUserAsync(Guid userId);

    // Throws NotFoundException if there is no such user.
    Task<UserDto> GetByIdAsync(Guid id);

    // Case-insensitive match on username. Returns at most 20 users.
    Task<List<UserDto>> SearchAsync(string query);
}
