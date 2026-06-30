using Microsoft.Extensions.Caching.Memory;

namespace DiscordBot.Infrastructure.Auth;

public interface IAuthCodeService
{
    /// <summary>Stores JWT briefly and returns a one-time code for the dashboard to exchange.</summary>
    string CreateExchangeCode(string accessToken);

    /// <summary>Returns the JWT and removes the code. Null if invalid or expired.</summary>
    string? ConsumeExchangeCode(string code);
}

public class AuthCodeService : IAuthCodeService
{
    private const string CachePrefix = "auth-code:";
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(2);

    private readonly IMemoryCache _cache;

    public AuthCodeService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string CreateExchangeCode(string accessToken)
    {
        var code = Guid.NewGuid().ToString("N");
        _cache.Set(CachePrefix + code, accessToken, CodeLifetime);
        return code;
    }

    public string? ConsumeExchangeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var key = CachePrefix + code;
        if (!_cache.TryGetValue(key, out string? accessToken))
        {
            return null;
        }

        _cache.Remove(key);
        return accessToken;
    }
}
