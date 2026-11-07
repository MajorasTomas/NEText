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
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();

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

        modelBuilder.Entity<FriendRequest>(entity =>
        {
            entity.HasKey(request => request.Id);

            // Two relationships to the same table (Users), so each one is spelled out:
            // "each FriendRequest has one From user, using the FromUserId column as the foreign key".
            // WithMany() = a user can have many requests (we don't need a list property on User for that).
            // Cascade = if a user is deleted, their friend requests are deleted too.
            entity.HasOne(request => request.From)
                .WithMany()
                .HasForeignKey(request => request.FromUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(request => request.To)
                .WithMany()
                .HasForeignKey(request => request.ToUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Store the enum as readable text ("Pending") instead of a number (0).
            entity.Property(request => request.Status)
                .HasConversion<string>()
                .HasMaxLength(16);

            // At most one row per direction. The service also checks the reverse direction,
            // so in practice there is one row per pair of users.
            entity.HasIndex(request => new { request.FromUserId, request.ToUserId }).IsUnique();

            // Speeds up "requests sent to me" (the index above already covers lookups by FromUserId).
            entity.HasIndex(request => request.ToUserId);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(conversation => conversation.Id);

            entity.Property(conversation => conversation.Type)
                .HasConversion<string>()
                .HasMaxLength(16);

            entity.Property(conversation => conversation.Name).HasMaxLength(64);
        });

        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            // Composite primary key: the pair must be unique, so a user can't be in the same conversation twice.
            entity.HasKey(participant => new { participant.ConversationId, participant.UserId });

            entity.Property(participant => participant.Role)
                .HasConversion<string>()
                .HasMaxLength(16);

            entity.Property(participant => participant.Nickname).HasMaxLength(32);

            // Here both sides have navigation properties: Conversation.Participants <-> Participant.Conversation.
            entity.HasOne(participant => participant.Conversation)
                .WithMany(conversation => conversation.Participants)
                .HasForeignKey(participant => participant.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(participant => participant.User)
                .WithMany()
                .HasForeignKey(participant => participant.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // "My conversations" looks participants up by UserId (the primary key starts with ConversationId).
            entity.HasIndex(participant => participant.UserId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(message => message.Id);

            entity.Property(message => message.Text).HasMaxLength(4000);

            entity.HasOne(message => message.Conversation)
                .WithMany(conversation => conversation.Messages)
                .HasForeignKey(message => message.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(message => message.Sender)
                .WithMany()
                .HasForeignKey(message => message.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Loading a conversation's messages newest-first (pagination) uses exactly this index.
            entity.HasIndex(message => new { message.ConversationId, message.SentAtUtc });
        });
    }
}
