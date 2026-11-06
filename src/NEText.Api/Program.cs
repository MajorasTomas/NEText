using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NEText.Api.Auth;
using NEText.Api.Data;
using NEText.Api.Entities;
using NEText.Api.Errors;
using NEText.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Register services (dependency injection container) ----------

builder.Services.AddControllers()
    // Enums in JSON as text ("Pending") instead of numbers (0), in both requests and responses.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Generated links (e.g. the Location header) are lowercase: /api/users instead of /api/Users
builder.Services.AddRouting(options => options.LowercaseUrls = true);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Errors: every error response uses the standard Problem Details JSON format,
// and GlobalExceptionHandler maps our AppExceptions (NotFound, Conflict, ...) to status codes.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Database: AppDbContext talks to PostgreSQL. Connection string comes from appsettings.Development.json.
// AddDbContext registers it as Scoped = one DbContext per HTTP request.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// JWT settings: Issuer/Audience/ExpiresMinutes from appsettings.json, Key from user-secrets.
// Configure<T> makes them injectable as IOptions<JwtOptions> (used by TokenService).
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);
var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32)
{
    // Fail at startup with a clear message instead of confusing errors at the first login.
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 characters. Set it with: " +
        "dotnet user-secrets set \"Jwt:Key\" \"<at least 32 random characters>\" --project src/NEText.Api");
}

// Authentication: on every request, read "Authorization: Bearer <token>" and validate the token.
// If valid, its claims become the controller's "User". If missing/invalid, [Authorize] endpoints return 401.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names as they are in the token ("sub" stays "sub", instead of being renamed to a long URL).
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.GetSigningKey(),  // signature must match our secret key
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,                // "iss" must be NEText
            ValidateAudience = true,
            ValidAudience = jwt.Audience,            // "aud" must be NEText
            ValidateLifetime = true,                 // "exp" must be in the future
            ClockSkew = TimeSpan.FromSeconds(30),    // small tolerance for clock differences (default is 5 minutes)
            NameClaimType = JwtRegisteredClaimNames.UniqueName // User.Identity.Name = username
        };
    });
builder.Services.AddAuthorization();

// Singleton: PasswordHasher has no state, so one shared instance is enough.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
// Singleton: TokenService only needs the (fixed) JWT settings.
builder.Services.AddSingleton<ITokenService, TokenService>();

// Our own services. Scoped because they use AppDbContext (also scoped).
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFriendService, FriendService>();

var app = builder.Build();

// ---------- 2. Configure the HTTP request pipeline (middleware, runs in order) ----------

// First in the pipeline, so it catches exceptions thrown by anything after it (including controllers).
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // API description at /openapi/v1.json (can be imported into Postman)
    app.MapOpenApi();
}
else
{
    // Locally we call plain http://localhost:5180 from Postman, so only force HTTPS outside development.
    app.UseHttpsRedirection();
}

// Order matters: first find out WHO is calling (authentication), then check if they're ALLOWED (authorization).
app.UseAuthentication();
app.UseAuthorization();

// Connects routes like /api/users/{id} to the controller methods.
app.MapControllers();

app.Run();
