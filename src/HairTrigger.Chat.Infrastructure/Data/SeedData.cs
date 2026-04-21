using HairTrigger.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HairTrigger.Chat.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ChatDbContext>>();

        try
        {
            // Ensure the database exists and tables are accessible
            // NOTE: Migrations are managed by backend-isj. This only seeds test data.
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogWarning("Cannot connect to chat database. Ensure backend-isj has run migrations.");
                return;
            }

            // Seed test rooms if none exist
            if (!await context.ChatRooms.AnyAsync())
            {
                var rooms = new List<ChatRoom>
                {
                    new()
                    {
                        Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        RoomType = ChatRoomType.Consultation,
                        SessionReferenceId = null,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                        RoomType = ChatRoomType.SupportGroup,
                        SessionReferenceId = null,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                        RoomType = ChatRoomType.Emergency,
                        SessionReferenceId = null,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.ChatRooms.AddRange(rooms);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} test chat rooms", rooms.Count);
            }

            // Seed test participants (using placeholder user reference IDs from primary DB)
            if (!await context.ChatRoomParticipants.AnyAsync())
            {
                var testUserIds = new[]
                {
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("33333333-3333-3333-3333-333333333333")
                };

                var roomIds = await context.ChatRooms.Select(r => r.Id).ToListAsync();
                var participants = new List<ChatRoomParticipant>();

                foreach (var roomId in roomIds)
                {
                    foreach (var userId in testUserIds)
                    {
                        participants.Add(new ChatRoomParticipant
                        {
                            Id = Guid.NewGuid(),
                            RoomId = roomId,
                            UserReferenceId = userId,
                            Role = "client",
                            JoinedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                context.ChatRoomParticipants.AddRange(participants);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} test participants", participants.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the chat database");
        }
    }
}
