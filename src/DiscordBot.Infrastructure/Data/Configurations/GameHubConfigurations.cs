using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class PlatformGameDefinitionConfiguration : IEntityTypeConfiguration<PlatformGameDefinition>
{
    public static readonly Guid QuizId = Guid.Parse("8f763e4f-d09e-48f5-b77b-406ecef81f98");
    public static readonly Guid RouletteId = Guid.Parse("77cfca31-9574-4f30-8ac5-e87d1eb65663");

    public void Configure(EntityTypeBuilder<PlatformGameDefinition> b)
    {
        b.ToTable("PlatformGameDefinitions"); b.HasKey(x => x.Id); b.HasIndex(x => x.Key).IsUnique();
        b.Property(x => x.Key).HasMaxLength(64).IsRequired(); b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000); b.Property(x => x.IconUrl).HasMaxLength(1000);
        b.Property(x => x.ActivityRoute).HasMaxLength(300).IsRequired(); b.Property(x => x.RequiredPlan).HasMaxLength(32).IsRequired();
        b.Property(x => x.PlayMode).HasConversion<string>().HasMaxLength(24).HasDefaultValue(GamePlayMode.Solo).IsRequired();
        var seededAt = new DateTimeOffset(2026, 7, 6, 0, 0, 0, TimeSpan.Zero);
        b.HasData(new PlatformGameDefinition
        {
            Id = QuizId, Key = "quiz", Name = "تحدي الأسئلة", Description = "جاوب على الأسئلة واكسب نقاط.",
            ActivityRoute = "/games/quiz", RequiredPlan = "free", PlayMode = GamePlayMode.Solo, IsEnabledGlobally = true,
            DefaultPointsPerWin = 10, DefaultCooldownSeconds = 30, DefaultMaxPlaysPerDay = 10,
            SupportsScores = true, SupportsLeaderboard = true, SupportsResultPublishing = true,
            CreatedAt = seededAt, UpdatedAt = seededAt
        });
        b.HasData(new PlatformGameDefinition
        {
            Id = RouletteId, Key = "roulette", Name = "الروليت", Description = "لعبة جماعية تعتمد على الحظ والتحدي بين الأعضاء.",
            ActivityRoute = "/games/roulette", RequiredPlan = "pro", PlayMode = GamePlayMode.Multiplayer, IsEnabledGlobally = true,
            DefaultPointsPerWin = 0, DefaultCooldownSeconds = 30, DefaultMaxPlaysPerDay = 10,
            SupportsScores = true, SupportsLeaderboard = true, SupportsResultPublishing = true,
            CreatedAt = seededAt, UpdatedAt = seededAt
        });
    }
}

public class RouletteGuildSettingsConfiguration : IEntityTypeConfiguration<RouletteGuildSettings>
{
    public void Configure(EntityTypeBuilder<RouletteGuildSettings> b)
    {
        b.ToTable("RouletteGuildSettings"); b.HasKey(x => x.Id); b.HasIndex(x => x.GuildId).IsUnique();
        b.HasOne(x => x.Guild).WithOne(x => x.RouletteSettings).HasForeignKey<RouletteGuildSettings>(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GameWalletConfiguration : IEntityTypeConfiguration<GameWallet>
{
    public void Configure(EntityTypeBuilder<GameWallet> b)
    {
        b.ToTable("GameWallets", table => table.HasCheckConstraint("CK_GameWallets_Balance_NonNegative", "\"Balance\" >= 0")); b.HasKey(x => x.Id); b.HasIndex(x => new { x.GuildId, x.UserDiscordId }).IsUnique();
        b.Property(x => x.UserDiscordId).HasMaxLength(32).IsRequired();
        b.HasOne(x => x.Guild).WithMany(x => x.GameWallets).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GameWalletTransactionConfiguration : IEntityTypeConfiguration<GameWalletTransaction>
{
    public void Configure(EntityTypeBuilder<GameWalletTransaction> b)
    {
        b.ToTable("GameWalletTransactions"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.GuildId, x.UserDiscordId, x.CreatedAt });
        b.HasIndex(x => x.ReferenceId); b.HasIndex(x => new { x.ReferenceId, x.UserDiscordId, x.Type }).IsUnique(); b.Property(x => x.UserDiscordId).HasMaxLength(32).IsRequired();
        b.Property(x => x.Type).HasMaxLength(64).IsRequired(); b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.Guild).WithMany(x => x.GameWalletTransactions).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RouletteRoomConfiguration : IEntityTypeConfiguration<RouletteRoom>
{
    public void Configure(EntityTypeBuilder<RouletteRoom> b)
    {
        b.ToTable("RouletteRooms"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.GuildId, x.ChannelDiscordId, x.Status });
        b.Property(x => x.ChannelDiscordId).HasMaxLength(32).IsRequired(); b.Property(x => x.HostUserDiscordId).HasMaxLength(32).IsRequired();
        b.Property(x => x.HostUsername).HasMaxLength(80).IsRequired(); b.Property(x => x.Status).HasMaxLength(24).IsRequired(); b.Property(x => x.InviteMessageDiscordId).HasMaxLength(32);
        b.Property(x => x.CurrentTurnUserDiscordId).HasMaxLength(32); b.Property(x => x.PendingTargetUserDiscordId).HasMaxLength(32);
        b.Property(x => x.PendingActionStatus).HasMaxLength(32); b.Property(x => x.LastSpinResultJson).HasColumnType("jsonb");
        b.HasOne(x => x.Guild).WithMany(x => x.RouletteRooms).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PlatformGameDefinition).WithMany().HasForeignKey(x => x.PlatformGameDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GamePowerUpDefinitionConfiguration : IEntityTypeConfiguration<GamePowerUpDefinition>
{
    public static readonly Guid ShieldId = Guid.Parse("15125967-74cc-4809-9397-2c5d30f38bd8");
    public static readonly Guid ReverseId = Guid.Parse("5bf04af4-0d20-490c-aa9f-a82cc6cb02b7");
    public static readonly Guid ReSpinId = Guid.Parse("676c428c-fc17-44ee-8bef-a2ad8ed4ad88");
    public void Configure(EntityTypeBuilder<GamePowerUpDefinition> b)
    {
        b.ToTable("GamePowerUpDefinitions"); b.HasKey(x => x.Id); b.HasIndex(x => x.Key).IsUnique();
        b.Property(x => x.Key).HasMaxLength(32).IsRequired(); b.Property(x => x.Name).HasMaxLength(100).IsRequired(); b.Property(x => x.Description).HasMaxLength(500).IsRequired(); b.Property(x => x.Icon).HasMaxLength(32).IsRequired();
        var seededAt = new DateTimeOffset(2026, 7, 6, 0, 0, 0, TimeSpan.Zero);
        b.HasData(
            new GamePowerUpDefinition { Id = ShieldId, Key = "shield", Name = "الدرع", Description = "يحميك من الإقصاء مرة واحدة.", Icon = "🛡️", DefaultPrice = 100, IsEnabledGlobally = true, CreatedAt = seededAt, UpdatedAt = seededAt },
            new GamePowerUpDefinition { Id = ReverseId, Key = "reverse", Name = "عكس الهجمة", Description = "يعكس الإقصاء على اللاعب الذي لف العجلة.", Icon = "🔁", DefaultPrice = 150, IsEnabledGlobally = true, CreatedAt = seededAt, UpdatedAt = seededAt },
            new GamePowerUpDefinition { Id = ReSpinId, Key = "respin", Name = "إعادة اللف", Description = "يعيد تدوير العجلة مرة واحدة.", Icon = "🎡", DefaultPrice = 120, IsEnabledGlobally = true, CreatedAt = seededAt, UpdatedAt = seededAt });
    }
}

public class GuildPowerUpSettingConfiguration : IEntityTypeConfiguration<GuildPowerUpSetting>
{
    public void Configure(EntityTypeBuilder<GuildPowerUpSetting> b)
    {
        b.ToTable("GuildPowerUpSettings"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.GuildId, x.GamePowerUpDefinitionId }).IsUnique();
        b.HasOne(x => x.Guild).WithMany(x => x.PowerUpSettings).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.GamePowerUpDefinition).WithMany(x => x.GuildSettings).HasForeignKey(x => x.GamePowerUpDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PlayerPowerUpInventoryConfiguration : IEntityTypeConfiguration<PlayerPowerUpInventory>
{
    public void Configure(EntityTypeBuilder<PlayerPowerUpInventory> b)
    {
        b.ToTable("PlayerPowerUpInventories", table => table.HasCheckConstraint("CK_PlayerPowerUpInventories_Quantity_NonNegative", "\"Quantity\" >= 0")); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.GuildId, x.UserDiscordId, x.GamePowerUpDefinitionId }).IsUnique(); b.Property(x => x.UserDiscordId).HasMaxLength(32).IsRequired();
        b.HasOne(x => x.Guild).WithMany(x => x.PowerUpInventories).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.GamePowerUpDefinition).WithMany(x => x.Inventories).HasForeignKey(x => x.GamePowerUpDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoulettePowerUpUsageConfiguration : IEntityTypeConfiguration<RoulettePowerUpUsage>
{
    public void Configure(EntityTypeBuilder<RoulettePowerUpUsage> b)
    {
        b.ToTable("RoulettePowerUpUsages"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.RouletteRoomId, x.UserDiscordId, x.GamePowerUpDefinitionId, x.RoundNumber }).IsUnique();
        b.Property(x => x.UserDiscordId).HasMaxLength(32).IsRequired(); b.Property(x => x.ResultJson).HasColumnType("jsonb").IsRequired();
        b.HasOne(x => x.RouletteRoom).WithMany(x => x.PowerUpUsages).HasForeignKey(x => x.RouletteRoomId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.GamePowerUpDefinition).WithMany().HasForeignKey(x => x.GamePowerUpDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RouletteRoomPlayerConfiguration : IEntityTypeConfiguration<RouletteRoomPlayer>
{
    public void Configure(EntityTypeBuilder<RouletteRoomPlayer> b)
    {
        b.ToTable("RouletteRoomPlayers"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.RouletteRoomId, x.UserDiscordId }).IsUnique();
        b.Property(x => x.UserDiscordId).HasMaxLength(32).IsRequired(); b.Property(x => x.Username).HasMaxLength(80).IsRequired();
        b.HasOne(x => x.RouletteRoom).WithMany(x => x.Players).HasForeignKey(x => x.RouletteRoomId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RouletteRoundActionConfiguration : IEntityTypeConfiguration<RouletteRoundAction>
{
    public void Configure(EntityTypeBuilder<RouletteRoundAction> b)
    {
        b.ToTable("RouletteRoundActions"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.RouletteRoomId, x.RoundNumber });
        b.Property(x => x.ActorUserDiscordId).HasMaxLength(32).IsRequired(); b.Property(x => x.TargetUserDiscordId).HasMaxLength(32);
        b.Property(x => x.ActionType).HasMaxLength(32).IsRequired(); b.Property(x => x.DataJson).HasColumnType("jsonb").IsRequired();
        b.HasOne(x => x.RouletteRoom).WithMany(x => x.Actions).HasForeignKey(x => x.RouletteRoomId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RouletteJoinIntentConfiguration : IEntityTypeConfiguration<RouletteJoinIntent>
{
    public void Configure(EntityTypeBuilder<RouletteJoinIntent> b)
    {
        b.ToTable("RouletteJoinIntents"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.UserDiscordId, x.Status, x.ExpiresAt });
        b.Property(x => x.UserDiscordId).HasMaxLength(32).IsRequired(); b.Property(x => x.ChannelDiscordId).HasMaxLength(32).IsRequired(); b.Property(x => x.Status).HasMaxLength(24).IsRequired();
        b.HasOne(x => x.Guild).WithMany(x => x.RouletteJoinIntents).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.RouletteRoom).WithMany(x => x.JoinIntents).HasForeignKey(x => x.RouletteRoomId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RoulettePublishActionConfiguration : IEntityTypeConfiguration<RoulettePublishAction>
{
    public void Configure(EntityTypeBuilder<RoulettePublishAction> b)
    {
        b.ToTable("RoulettePublishActions"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.Property(x => x.ChannelDiscordId).HasMaxLength(32).IsRequired(); b.Property(x => x.Type).HasMaxLength(32).IsRequired(); b.Property(x => x.Status).HasMaxLength(24).IsRequired();
        b.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired(); b.Property(x => x.ErrorMessage).HasMaxLength(2000);
        b.HasOne(x => x.Guild).WithMany(x => x.RoulettePublishActions).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RouletteRoom).WithMany(x => x.PublishActions).HasForeignKey(x => x.RouletteRoomId).OnDelete(DeleteBehavior.Cascade);
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
