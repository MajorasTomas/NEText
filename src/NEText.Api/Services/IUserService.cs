using NEText.Api.Dtos;

namespace NEText.Api.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterRequest request);

    Task<LoginResponse> LoginAsync(LoginRequest request);

    Task<CurrentUserDto> GetCurrentUserAsync(Guid userId);

    Task<UserDto> GetByIdAsync(Guid id);

    Task<List<UserDto>> SearchAsync(string query);
}
