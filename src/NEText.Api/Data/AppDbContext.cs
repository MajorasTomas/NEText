using Microsoft.EntityFrameworkCore;

namespace NEText.Api.Data;

// The gateway to the database. Each DbSet<T> is a table; EF Core turns LINQ queries on it into SQL.
public class AppDbContext : DbContext
{
    // The options (which database, connection string) are passed in by DI, configured in Program.cs.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Tables go here, one line each, e.g.:
    // public DbSet<User> Users => Set<User>();

    // Fine-tune how entities map to tables (column sizes, required fields, indexes, relationships).
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}
