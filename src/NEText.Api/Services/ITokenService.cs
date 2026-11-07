using NEText.Api.Entities;

namespace NEText.Api.Services;

public interface ITokenService
{
    AccessToken CreateToken(User user);
}

public record AccessToken(string Token, DateTime ExpiresAtUtc);
