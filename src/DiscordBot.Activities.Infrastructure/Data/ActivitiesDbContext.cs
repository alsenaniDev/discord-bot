using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Activities.Infrastructure.Data;

public class ActivitiesDbContext(DbContextOptions<ActivitiesDbContext> options) : DbContext(options)
{
    public DbSet<ActivitySession> ActivitySessions => Set<ActivitySession>();
    public DbSet<ActivityPlayer> ActivityPlayers => Set<ActivityPlayer>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GamePlayer> GamePlayers => Set<GamePlayer>();
    public DbSet<GameEvent> GameEvents => Set<GameEvent>();
    public DbSet<GameResult> GameResults => Set<GameResult>();
    public DbSet<GameWalletTransaction> GameWalletTransactions => Set<GameWalletTransaction>();
    public DbSet<RouletteGameSession> RouletteGameSessions => Set<RouletteGameSession>();
    public DbSet<RoulettePlayer> RoulettePlayers => Set<RoulettePlayer>();
    public DbSet<RouletteRound> RouletteRounds => Set<RouletteRound>();
    public DbSet<RouletteBet> RouletteBets => Set<RouletteBet>();
    public DbSet<RoulettePayout> RoulettePayouts => Set<RoulettePayout>();
    public DbSet<RouletteJoinIntent> RouletteJoinIntents => Set<RouletteJoinIntent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ActivitiesDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ActivitiesEntity>())
        {
            if (entry.State == EntityState.Modified) entry.Entity.UpdatedAtUtc = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
