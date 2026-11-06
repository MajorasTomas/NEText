using NEText.Api.Entities;

namespace NEText.Api.Services;

public interface ITokenService
{
    // Creates a signed JWT for this user.
    AccessToken CreateToken(User user);
}

public record AccessToken(string Token, DateTime ExpiresAtUtc);
