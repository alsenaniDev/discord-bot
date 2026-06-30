using DiscordBot.Domain.Entities;

namespace DiscordBot.Infrastructure.Auth;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
