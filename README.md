# NEText
messenger app

## Running locally

Requirements: .NET 10 SDK, Docker Desktop, `dotnet-ef` tool (`dotnet tool install --global dotnet-ef`).

One-time setup: the JWT signing key is kept in user-secrets (outside the repo). Any random string of 32+ characters works:

```bash
dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>" --project src/NEText.Api
```

```bash
# 1. Start PostgreSQL (in Docker)
docker compose up -d

# 2. Create/update the database tables (needed after pulling new migrations)
dotnet ef database update --project src/NEText.Api

# 3. Run the API -> http://localhost:5180
dotnet run --project src/NEText.Api
```

API documentation (Development only): http://localhost:5180/swagger lists every endpoint and lets you try them. Log in with `POST /api/auth/login`, copy the `accessToken`, click **Authorize** and paste it to call the protected endpoints. The raw OpenAPI document is at http://localhost:5180/openapi/v1.json.

Testing: import `postman/NEText.postman_collection.json` into Postman. Register, then Login (saves the token used by all other requests).

Stop the database with `docker compose down` (data is kept). `docker compose down -v` deletes all data.

## Database changes

After changing entities or `AppDbContext`:

```bash
dotnet ef migrations add <DescriptiveName> --project src/NEText.Api --output-dir Data/Migrations
dotnet ef database update --project src/NEText.Api
```
