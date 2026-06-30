using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DiscordBot.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_options.Secret) || _options.Secret.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must be at least 32 characters.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("discord_id", user.DiscordUserId),
            new(JwtRegisteredClaimNames.UniqueName, user.Username)
        };

        if (!string.IsNullOrWhiteSpace(user.GlobalName))
        {
            claims.Add(new Claim("global_name", user.GlobalName));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
