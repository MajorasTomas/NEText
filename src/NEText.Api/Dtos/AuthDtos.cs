using System.ComponentModel.DataAnnotations;

namespace NEText.Api.Dtos;

// Body of POST /api/auth/register.
// [ApiController] checks these attributes automatically and returns 400 with details if any fail.
public record RegisterRequest(
    [Required, EmailAddress, MaxLength(254)]
    string Email,

    [Required, MinLength(3), MaxLength(32)]
    [RegularExpression("^[a-zA-Z0-9_.]+$", ErrorMessage = "Username may only contain letters, digits, '_' and '.'.")]
    string Username,

    [Required, MinLength(8), MaxLength(100)]
    string Password,

    // Optional; defaults to the username as typed (e.g. "Alice")
    [MaxLength(64)]
    string? DisplayName
);
