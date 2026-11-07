using Microsoft.EntityFrameworkCore;
using NEText.Api.Entities;

namespace NEText.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);

            entity.Property(user => user.Username).HasMaxLength(32);
            entity.Property(user => user.Email).HasMaxLength(254);

            entity.HasIndex(user => user.Username).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<FriendRequest>(entity =>
        {
            entity.HasKey(request => request.Id);

            entity.HasOne(request => request.From)
                .WithMany()
                .HasForeignKey(request => request.FromUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(request => request.To)
                .WithMany()
                .HasForeignKey(request => request.ToUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(request => request.Status)
                .HasConversion<string>()
                .HasMaxLength(16);

            entity.HasIndex(request => new { request.FromUserId, request.ToUserId }).IsUnique();

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
            entity.HasKey(participant => new { participant.ConversationId, participant.UserId });

            entity.Property(participant => participant.Role)
                .HasConversion<string>()
                .HasMaxLength(16);

            entity.Property(participant => participant.Nickname).HasMaxLength(32);

            entity.HasOne(participant => participant.Conversation)
                .WithMany(conversation => conversation.Participants)
                .HasForeignKey(participant => participant.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(participant => participant.User)
                .WithMany()
                .HasForeignKey(participant => participant.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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

            entity.HasIndex(message => new { message.ConversationId, message.SentAtUtc });
        });
    }
}
