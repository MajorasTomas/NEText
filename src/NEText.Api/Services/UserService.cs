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
    private readonly ITokenService _tokens;

    public UserService(AppDbContext db, IPasswordHasher<User> passwordHasher, ITokenService tokens)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim().ToLowerInvariant();

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
            Id = Guid.CreateVersion7(),
            Username = username,
            Email = email,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("Username or email is already taken.");
        }

        return ToDto(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        var result = user is null
            ? PasswordVerificationResult.Failed
            : _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (user is null || result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            await _db.SaveChangesAsync();
        }

        var token = _tokens.CreateToken(user);
        return new LoginResponse(token.Token, token.ExpiresAtUtc, ToCurrentUserDto(user));
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new UnauthorizedException("User from token no longer exists.");

        return ToCurrentUserDto(user);
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

        return await _db.Users
            .Where(u => EF.Functions.ILike(u.Username, pattern))
            .OrderBy(u => u.Username)
            .Take(MaxSearchResults)
            .Select(u => new UserDto(u.Id, u.Username, u.CreatedAtUtc))
            .ToListAsync();
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Username, u.CreatedAtUtc);

    private static CurrentUserDto ToCurrentUserDto(User u) => new(u.Id, u.Username, u.Email, u.CreatedAtUtc);
}
