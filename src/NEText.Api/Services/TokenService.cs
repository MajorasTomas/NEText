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

    // IOptions<JwtOptions> = the "Jwt" config section, bound to JwtOptions (registered in Program.cs).
    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessToken CreateToken(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,     // "iss"
            Audience = _options.Audience, // "aud"
            Expires = expiresAtUtc,       // "exp" ("iat" and "nbf" = now are added automatically)

            // The payload: facts about the user. Readable by anyone who has the token, so nothing secret.
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),        // "sub": who this token is for
                [JwtRegisteredClaimNames.UniqueName] = user.Username       // "unique_name"
            },

            // Signature = HMAC-SHA256(header + "." + payload, secret key)
            SigningCredentials = new SigningCredentials(_options.GetSigningKey(), SecurityAlgorithms.HmacSha256)
        };

        // Produces "header.payload.signature"
        return new AccessToken(_handler.CreateToken(descriptor), expiresAtUtc);
    }
}
