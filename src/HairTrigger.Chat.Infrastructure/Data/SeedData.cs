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
            await context.Database.MigrateAsync();

            // Seed test users if none exist
            if (!await context.Users.AnyAsync())
            {
                var users = new List<User>
                {
                    new()
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Username = "alice",
                        DisplayName = "Alice Johnson",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        Username = "bob",
                        DisplayName = "Bob Smith",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                        Username = "charlie",
                        DisplayName = "Charlie Brown",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Users.AddRange(users);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} test users", users.Count);
            }

            // Seed test channels if none exist
            if (!await context.Channels.AnyAsync())
            {
                var channels = new List<Channel>
                {
                    new()
                    {
                        Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        Name = "general",
                        Description = "General discussion channel",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    new()
                    {
                        Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                        Name = "random",
                        Description = "Random chat and fun stuff",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    },
                    new()
                    {
                        Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                        Name = "tech-talk",
                        Description = "Technology discussions",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    }
                };

                context.Channels.AddRange(channels);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} test channels", channels.Count);
            }

            // Add all test users to all channels
            if (!await context.ChannelMembers.AnyAsync())
            {
                var userIds = await context.Users.Select(u => u.Id).ToListAsync();
                var channelIds = await context.Channels.Select(c => c.Id).ToListAsync();

                var memberships = new List<ChannelMember>();
                foreach (var channelId in channelIds)
                {
                    foreach (var userId in userIds)
                    {
                        memberships.Add(new ChannelMember
                        {
                            ChannelId = channelId,
                            UserId = userId,
                            JoinedAt = DateTime.UtcNow,
                            Role = ChannelMemberRole.Member
                        });
                    }
                }

                context.ChannelMembers.AddRange(memberships);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} channel memberships", memberships.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
