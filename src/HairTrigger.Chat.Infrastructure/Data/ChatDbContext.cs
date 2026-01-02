using HairTrigger.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HairTrigger.Chat.Infrastructure.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ChannelMember> ChannelMembers => Set<ChannelMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<DeliveryStatus> DeliveryStatuses => Set<DeliveryStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // Channel configuration
        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Name);
        });

        // ChannelMember configuration (many-to-many join table)
        modelBuilder.Entity<ChannelMember>(entity =>
        {
            entity.HasKey(e => new { e.ChannelId, e.UserId });
            
            entity.HasOne(e => e.Channel)
                .WithMany(c => c.Members)
                .HasForeignKey(e => e.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.ChannelMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Offset).IsRequired();
            
            entity.HasOne(e => e.Channel)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.ChannelId);
            entity.HasIndex(e => new { e.ChannelId, e.Offset });
            entity.HasIndex(e => e.CreatedAt);
        });

        // DeliveryStatus configuration
        modelBuilder.Entity<DeliveryStatus>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.MessageId });
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.DeliveryStatuses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Message)
                .WithMany(m => m.DeliveryStatuses)
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
