using Microsoft.EntityFrameworkCore;
using NEText.Api.Entities;

namespace NEText.Api.Data;

// The gateway to the database. Each DbSet<T> is a table; EF Core turns LINQ queries on it into SQL.
public class AppDbContext : DbContext
{
    // The options (which database, connection string) are passed in by DI, configured in Program.cs.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    // Fine-tune how entities map to tables (column sizes, required fields, indexes, relationships).
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);

            entity.Property(user => user.Username).HasMaxLength(32);
            entity.Property(user => user.Email).HasMaxLength(254);

            // Unique indexes: the database itself refuses a second row with the same value.
            entity.HasIndex(user => user.Username).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
        });
    }
}
