using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using NEText.Api.Errors;

namespace NEText.Api.Auth;

// Extension method, so any controller can write: var myId = User.GetUserId();
// "User" in a controller is the ClaimsPrincipal built by the JWT middleware from the token's claims.
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        // "sub" (subject) holds the user's id; TokenService puts it there when logging in.
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedException("Token does not contain a valid user id.");
    }
}
