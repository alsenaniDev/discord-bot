export interface PlatformGameDefinition {
  id: string; key: string; name: string; description?: string | null; iconUrl?: string | null;
  activityRoute: string; requiredPlan: string; isEnabledGlobally: boolean;
  defaultPointsPerWin: number; defaultCooldownSeconds: number; defaultMaxPlaysPerDay: number;
  supportsScores: boolean; supportsLeaderboard: boolean; supportsResultPublishing: boolean;
}
export type SavePlatformGameDefinition = Omit<PlatformGameDefinition, 'id'>;
export interface GuildGamesSettings { guildId: string; isEnabled: boolean; gamesChannelDiscordId?: string | null; autoPostPanel: boolean; gamesPanelMessageDiscordId?: string | null; }
export interface UpdateGuildGamesSettings { isEnabled: boolean; gamesChannelDiscordId?: string | null; autoPostPanel: boolean; }
export interface GuildGame extends PlatformGameDefinition {
  isAvailableByPlan: boolean; isEnabledForGuild: boolean; pointsEnabled: boolean; pointsPerWin: number;
  cooldownSeconds: number; maxPlaysPerDay: number; publishResultAfterGame: boolean;
  publishLeaderboardAfterGame: boolean; publishOnlyWins: boolean; lockedReason?: string | null;
}
export interface UpdateGuildGameSetting { isEnabledForGuild: boolean; pointsEnabled: boolean; pointsPerWin: number; cooldownSeconds: number; maxPlaysPerDay: number; publishResultAfterGame: boolean; publishLeaderboardAfterGame: boolean; publishOnlyWins: boolean; }
export interface GameLeaderboardEntry { rank: number; userDiscordId: string; username: string; totalPoints: number; gamesPlayed: number; wins: number; losses: number; currentStreak: number; bestStreak: number; }
