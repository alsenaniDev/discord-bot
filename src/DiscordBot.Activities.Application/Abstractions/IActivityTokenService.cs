using DiscordBot.Activities.Application.Models;

namespace DiscordBot.Activities.Application.Abstractions;

public interface IActivityTokenService
{
    ActivityAuthResponse CreateToken(TrustedDiscordUser user);
}
