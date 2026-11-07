using System.ComponentModel.DataAnnotations;

namespace NEText.Api.Dtos;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(254)]
    string Email,

    [Required, MinLength(3), MaxLength(32)]
    [RegularExpression("^[a-zA-Z0-9_.]+$", ErrorMessage = "Username may only contain letters, digits, '_' and '.'.")]
    string Username,

    [Required, MinLength(8), MaxLength(100)]
    string Password
);

public record LoginRequest(
    [Required] string Email,
    [Required] string Password
);

public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, CurrentUserDto User);
