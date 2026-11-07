using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace NEText.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public int ExpiresMinutes { get; set; } = 60;

    public SymmetricSecurityKey GetSigningKey() => new(Encoding.UTF8.GetBytes(Key));
}
