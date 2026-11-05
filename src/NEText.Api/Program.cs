using Microsoft.EntityFrameworkCore;
using NEText.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Register services (dependency injection container) ----------

builder.Services.AddControllers();
// Generated links (e.g. the Location header) are lowercase: /api/users instead of /api/Users
builder.Services.AddRouting(options => options.LowercaseUrls = true);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Database: AppDbContext talks to PostgreSQL. Connection string comes from appsettings.Development.json.
// AddDbContext registers it as Scoped = one DbContext per HTTP request.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Our own services go here, e.g.:
// builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// ---------- 2. Configure the HTTP request pipeline (middleware, runs in order) ----------

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

// Connects routes like /api/health to the controller methods.
app.MapControllers();

app.Run();
