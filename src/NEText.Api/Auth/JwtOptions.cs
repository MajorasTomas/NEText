using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace NEText.Api.Auth;

// The "Jwt" section of configuration, as a typed object.
// Issuer/Audience/ExpiresMinutes come from appsettings.json; Key comes from user-secrets (never committed).
public class JwtOptions
{
    public const string SectionName = "Jwt";

    // Who created the token ("iss" claim) and who it's meant for ("aud" claim).
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    // Secret used to sign tokens (HMAC-SHA256). Must be at least 32 characters.
    public string Key { get; set; } = string.Empty;

    public int ExpiresMinutes { get; set; } = 60;

    // Used both when creating tokens (TokenService) and when validating them (Program.cs), so they always match.
    public SymmetricSecurityKey GetSigningKey() => new(Encoding.UTF8.GetBytes(Key));
}
