using DiscordBot.Domain.Entities;

namespace DiscordBot.Infrastructure.Services;

public sealed class AuthResult
{
    public required User User { get; init; }
    public required string AccessToken { get; init; }
}

public interface IAuthService
{
    Task<AuthResult> SignInWithDiscordAsync(string code, string state, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
