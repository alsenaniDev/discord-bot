using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class PlatformGameDefinitionConfiguration : IEntityTypeConfiguration<PlatformGameDefinition>
{
    public static readonly Guid QuizId = Guid.Parse("8f763e4f-d09e-48f5-b77b-406ecef81f98");

    public void Configure(EntityTypeBuilder<PlatformGameDefinition> b)
    {
        b.ToTable("PlatformGameDefinitions"); b.HasKey(x => x.Id); b.HasIndex(x => x.Key).IsUnique();
        b.Property(x => x.Key).HasMaxLength(64).IsRequired(); b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000); b.Property(x => x.IconUrl).HasMaxLength(1000);
        b.Property(x => x.ActivityRoute).HasMaxLength(300).IsRequired(); b.Property(x => x.RequiredPlan).HasMaxLength(32).IsRequired();
        var seededAt = new DateTimeOffset(2026, 7, 6, 0, 0, 0, TimeSpan.Zero);
        b.HasData(new PlatformGameDefinition
        {
            Id = QuizId, Key = "quiz", Name = "تحدي الأسئلة", Description = "جاوب على الأسئلة واكسب نقاط.",
            ActivityRoute = "/games/quiz", RequiredPlan = "free", IsEnabledGlobally = true,
            DefaultPointsPerWin = 10, DefaultCooldownSeconds = 30, DefaultMaxPlaysPerDay = 10,
            SupportsScores = true, SupportsLeaderboard = true, SupportsResultPublishing = true,
            CreatedAt = seededAt, UpdatedAt = seededAt
        });
    }
}

public class GuildGamesSettingsConfiguration : IEntityTypeConfiguration<GuildGamesSettings>
{
    public void Configure(EntityTypeBuilder<GuildGamesSettings> b)
    {
        b.ToTable("GuildGamesSettings"); b.HasKey(x => x.Id); b.HasIndex(x => x.GuildId).IsUnique();
        b.Property(x => x.GamesChannelDiscordId).HasMaxLength(32); b.Property(x => x.GamesPanelMessageDiscordId).HasMaxLength(32);
        b.HasOne(x => x.Guild).WithOne(x => x.GamesSettings).HasForeignKey<GuildGamesSettings>(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GuildGameSettingConfiguration : IEntityTypeConfiguration<GuildGameSetting>
{
    public void Configure(EntityTypeBuilder<GuildGameSetting> b)
    {
        b.ToTable("GuildGameSettings"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.GuildId, x.PlatformGameDefinitionId }).IsUnique();
        b.HasOne(x => x.Guild).WithMany(x => x.GameSettings).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.PlatformGameDefinition).WithMany(x => x.GuildSettings).HasForeignKey(x => x.PlatformGameDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> b)
    {
        b.ToTable("GameSessions"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.GuildId, x.UserDiscordId, x.StartedAt });
        b.HasIndex(x => x.Status); b.Property(x => x.UserDiscordId).HasMaxLength(32).IsRequired();
        b.Property(x => x.ChannelDiscordId).HasMaxLength(32); b.Property(x => x.Username).HasMaxLength(256); b.Property(x => x.Status).HasMaxLength(24).IsRequired();
        b.HasOne(x => x.Guild).WithMany(x => x.GameSessions).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PlatformGameDefinition).WithMany(x => x.Sessions).HasForeignKey(x => x.PlatformGameDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GamePlayerConfiguration : IEntityTypeConfiguration<GamePlayer>
{
    public void Configure(EntityTypeBuilder<GamePlayer> b)
    {
        b.ToTable("GamePlayers"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.GuildId, x.UserDiscordId }).IsUnique();
        b.HasIndex(x => new { x.GuildId, x.TotalPoints }); b.Property(x => x.UserDiscordId).HasMaxLength(32).IsRequired(); b.Property(x => x.Username).HasMaxLength(256).IsRequired();
        b.HasOne(x => x.Guild).WithMany(x => x.GamePlayers).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GameContentConfiguration : IEntityTypeConfiguration<GameContent>
{
    public void Configure(EntityTypeBuilder<GameContent> b)
    {
        b.ToTable("GameContent"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.PlatformGameDefinitionId, x.GuildId, x.IsEnabled });
        b.Property(x => x.Title).HasMaxLength(200).IsRequired(); b.Property(x => x.DataJson).HasColumnType("jsonb").IsRequired();
        b.HasOne(x => x.Guild).WithMany(x => x.GameContent).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.PlatformGameDefinition).WithMany(x => x.Content).HasForeignKey(x => x.PlatformGameDefinitionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GameResultPublishActionConfiguration : IEntityTypeConfiguration<GameResultPublishAction>
{
    public void Configure(EntityTypeBuilder<GameResultPublishAction> b)
    {
        b.ToTable("GameResultPublishActions"); b.HasKey(x => x.Id); b.HasIndex(x => x.Status); b.HasIndex(x => x.GameSessionId);
        b.Property(x => x.ChannelDiscordId).HasMaxLength(32).IsRequired(); b.Property(x => x.Type).HasMaxLength(32).IsRequired();
        b.Property(x => x.Status).HasMaxLength(24).IsRequired(); b.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired(); b.Property(x => x.ErrorMessage).HasMaxLength(2000);
        b.HasOne(x => x.Guild).WithMany(x => x.GameResultPublishActions).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.GameSession).WithMany(x => x.PublishActions).HasForeignKey(x => x.GameSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
