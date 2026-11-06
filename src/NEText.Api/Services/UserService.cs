using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NEText.Api.Data;
using NEText.Api.Dtos;
using NEText.Api.Entities;
using NEText.Api.Errors;
using Npgsql;

namespace NEText.Api.Services;

public class UserService : IUserService
{
    private const int MaxSearchResults = 20;

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request)
    {
        // Normalize, so "Alice@Mail.com " and "alice@mail.com" count as the same email.
        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim().ToLowerInvariant();

        // Friendly error messages for the common case.
        if (await _db.Users.AnyAsync(u => u.Username == username))
        {
            throw new ConflictException("Username is already taken.");
        }
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            throw new ConflictException("Email is already registered.");
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(), // time-ordered GUID: unique and index-friendly
            Username = username,
            Email = email,
            CreatedAtUtc = DateTime.UtcNow
        };
        // Generates a random salt and hashes the password with it; both end up in this one string.
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Safety net: two requests registered the same name at the same moment, so both passed
            // the checks above. The unique index in the database rejected the second one.
            throw new ConflictException("Username or email is already taken.");
        }

        return ToDto(user);
    }

    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id)
            ?? throw new NotFoundException($"User '{id}' was not found.");

        return ToDto(user);
    }

    public async Task<List<UserDto>> SearchAsync(string query)
    {
        var pattern = $"%{query.Trim()}%";

        // ILIKE = case-insensitive LIKE (PostgreSQL). Becomes:
        // WHERE "Username" ILIKE @pattern ORDER BY "Username" LIMIT 20
        return await _db.Users
            .Where(u => EF.Functions.ILike(u.Username, pattern))
            .OrderBy(u => u.Username)
            .Take(MaxSearchResults)
            .Select(u => new UserDto(u.Id, u.Username, u.CreatedAtUtc))
            .ToListAsync();
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Username, u.CreatedAtUtc);
}
