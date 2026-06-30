using DiscordBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IPlatformAdminService
{
    Task<bool> IsAdminAsync(string discordUserId, CancellationToken cancellationToken = default);
}

public class PlatformAdminService : IPlatformAdminService
{
    private readonly AppDbContext _dbContext;

    public PlatformAdminService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> IsAdminAsync(string discordUserId, CancellationToken cancellationToken = default) =>
        _dbContext.PlatformAdmins
            .AsNoTracking()
            .AnyAsync(a => a.DiscordUserId == discordUserId, cancellationToken);
}
