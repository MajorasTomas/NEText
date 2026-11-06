using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NEText.Api.Data;
using NEText.Api.Entities;
using NEText.Api.Errors;
using NEText.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Register services (dependency injection container) ----------

builder.Services.AddControllers();
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

// Singleton: PasswordHasher has no state, so one shared instance is enough.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// Our own services. Scoped because they use AppDbContext (also scoped).
builder.Services.AddScoped<IUserService, UserService>();

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

app.UseAuthorization();

// Connects routes like /api/users/{id} to the controller methods.
app.MapControllers();

app.Run();
