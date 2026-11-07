using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using NEText.Api.Errors;

namespace NEText.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedException("Token does not contain a valid user id.");
    }
}
