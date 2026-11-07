using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NEText.Api.Auth;
using NEText.Api.Entities;

namespace NEText.Api.Services;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly JsonWebTokenHandler _handler = new();

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessToken CreateToken(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAtUtc,

            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.UniqueName] = user.Username
            },

            SigningCredentials = new SigningCredentials(_options.GetSigningKey(), SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAtUtc);
    }
}
