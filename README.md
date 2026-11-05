# NEText
messenger app

## Running locally

Requirements: .NET 10 SDK, Docker Desktop, `dotnet-ef` tool (`dotnet tool install --global dotnet-ef`).

```bash
# 1. Start PostgreSQL (in Docker)
docker compose up -d

# 2. Create/update the database tables (needed after pulling new migrations)
dotnet ef database update --project src/NEText.Api

# 3. Run the API -> http://localhost:5180
dotnet run --project src/NEText.Api
```

Testing: import `postman/NEText.postman_collection.json` into Postman and run the collection.

Stop the database with `docker compose down` (data is kept). `docker compose down -v` deletes all data.

## Database changes

After changing entities or `AppDbContext`:

```bash
dotnet ef migrations add <DescriptiveName> --project src/NEText.Api --output-dir Data/Migrations
dotnet ef database update --project src/NEText.Api
```
