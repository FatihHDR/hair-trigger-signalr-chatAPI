using HairTrigger.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HairTrigger.Chat.Infrastructure.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
    public DbSet<ChatRoomParticipant> ChatRoomParticipants => Set<ChatRoomParticipant>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatTranscript> ChatTranscripts => Set<ChatTranscript>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enum value converters (store as lowercase strings to match backend-isj)
        var chatRoomTypeConverter = new ValueConverter<ChatRoomType, string>(
            v => v == ChatRoomType.SupportGroup ? "support_group"
               : v == ChatRoomType.Emergency ? "emergency"
               : "consultation",
            v => v == "support_group" ? ChatRoomType.SupportGroup
               : v == "emergency" ? ChatRoomType.Emergency
               : ChatRoomType.Consultation);

        var messageTypeConverter = new ValueConverter<MessageType, string>(
            v => v == MessageType.Image ? "image"
               : v == MessageType.File ? "file"
               : v == MessageType.System ? "system"
               : "text",
            v => v == "image" ? MessageType.Image
               : v == "file" ? MessageType.File
               : v == "system" ? MessageType.System
               : MessageType.Text);

        // ChatRoom → chat_rooms
        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.ToTable("chat_rooms");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoomType)
                .HasColumnName("room_type")
                .HasConversion(chatRoomTypeConverter)
                .HasMaxLength(50);
            entity.Property(e => e.SessionReferenceId).HasColumnName("session_reference_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // ChatRoomParticipant → chat_room_participants
        modelBuilder.Entity<ChatRoomParticipant>(entity =>
        {
            entity.ToTable("chat_room_participants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.UserReferenceId).HasColumnName("user_reference_id");
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(50);
            entity.Property(e => e.JoinedAt).HasColumnName("joined_at");
            entity.Property(e => e.LeftAt).HasColumnName("left_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.RoomId);

            entity.HasOne(e => e.Room)
                .WithMany(r => r.Participants)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatMessage → chat_messages
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.SenderReferenceId).HasColumnName("sender_reference_id");
            entity.Property(e => e.MessageType)
                .HasColumnName("message_type")
                .HasConversion(messageTypeConverter)
                .HasMaxLength(50);
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.RoomId);
            entity.HasIndex(e => e.SenderReferenceId);

            entity.HasOne(e => e.Room)
                .WithMany(r => r.Messages)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatTranscript → chat_transcripts
        modelBuilder.Entity<ChatTranscript>(entity =>
        {
            entity.ToTable("chat_transcripts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.GeneratedBy).HasColumnName("generated_by");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.GeneratedAt).HasColumnName("generated_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Room)
                .WithMany(r => r.Transcripts)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
