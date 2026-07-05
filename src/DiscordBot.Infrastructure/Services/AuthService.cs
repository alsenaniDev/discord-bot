using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Auth;
using DiscordBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IDiscordOAuthService _discordOAuthService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        AppDbContext dbContext,
        IDiscordOAuthService discordOAuthService,
        IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _discordOAuthService = discordOAuthService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResult> SignInWithDiscordAsync(
        string code,
        string state,
        CancellationToken cancellationToken = default)
    {
        var profile = await _discordOAuthService.ExchangeCodeAsync(code, state, cancellationToken);
        var user = await UpsertUserAsync(profile, cancellationToken);
        var accessToken = _jwtTokenService.GenerateToken(user);

        return new AuthResult
        {
            User = user,
            AccessToken = accessToken
        };
    }

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    private async Task<User> UpsertUserAsync(DiscordProfile profile, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.DiscordUserId == profile.DiscordUserId, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                DiscordUserId = profile.DiscordUserId,
                Username = profile.Username,
                GlobalName = profile.GlobalName,
                AvatarUrl = profile.AvatarUrl,
                DiscordAccessToken = profile.DiscordAccessToken,
                DiscordRefreshToken = profile.DiscordRefreshToken,
                DiscordTokenExpiresAtUtc = profile.DiscordTokenExpiresAtUtc,
                DiscordTokenScope = profile.DiscordTokenScope,
                LastLoginAt = DateTimeOffset.UtcNow
            };

            _dbContext.Users.Add(user);
        }
        else
        {
            user.Username = profile.Username;
            user.GlobalName = profile.GlobalName;
            user.AvatarUrl = profile.AvatarUrl;
            user.DiscordAccessToken = profile.DiscordAccessToken;
            user.DiscordRefreshToken = profile.DiscordRefreshToken;
            user.DiscordTokenExpiresAtUtc = profile.DiscordTokenExpiresAtUtc;
            user.DiscordTokenScope = profile.DiscordTokenScope;
            user.LastLoginAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
}
