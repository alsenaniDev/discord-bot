export interface ActivityGame { id: string; key: string; name: string; description?: string; iconUrl?: string; activityRoute: string; supportsScores: boolean; supportsLeaderboard: boolean; }
export interface LeaderboardEntry { rank: number; userDiscordId: string; username: string; totalPoints: number; gamesPlayed: number; wins: number; losses: number; currentStreak: number; bestStreak: number; }
export interface ActivityContext { guildDiscordId: string; channelDiscordId: string; gamesChannelDiscordId: string; games: ActivityGame[]; leaderboard: LeaderboardEntry[]; }
export interface StartSessionResponse { sessionId: string; gameKey: string; gameName: string; activityRoute: string; expiresAt: string; }
export interface CompleteSessionResponse { sessionId: string; pointsAwarded: number; player: LeaderboardEntry; }
export interface ActivityIdentity { accessToken: string; userId: string; username: string; guildId: string; channelId: string; }
