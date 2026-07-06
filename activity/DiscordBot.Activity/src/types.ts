export interface ActivityGame { id: string; key: string; name: string; description?: string; iconUrl?: string; activityRoute: string; playMode: 'Solo' | 'Multiplayer'; supportsScores: boolean; supportsLeaderboard: boolean; }
export interface LeaderboardEntry { rank: number; userDiscordId: string; username: string; totalPoints: number; gamesPlayed: number; wins: number; losses: number; currentStreak: number; bestStreak: number; }
export interface ActivityContext { guildDiscordId: string; channelDiscordId: string; gamesChannelDiscordId: string; games: ActivityGame[]; leaderboard: LeaderboardEntry[]; }
export interface StartSessionResponse { sessionId: string; gameKey: string; gameName: string; activityRoute: string; expiresAt: string; }
export interface CompleteSessionResponse { sessionId: string; pointsAwarded: number; player: LeaderboardEntry; }
export interface ActivityIdentity { accessToken: string; userId: string; username: string; guildId: string; channelId: string; }
export interface GameWallet { balance: number; }
export interface RoulettePlayer { userDiscordId: string; username: string; isHost: boolean; isAlive: boolean; position: number; eliminations: number; joinedAt: string; eliminatedAt?: string | null; }
export interface RouletteRoom { id: string; guildDiscordId: string; channelDiscordId: string; hostUserDiscordId: string; hostUsername: string; status: 'Waiting' | 'InProgress' | 'Completed' | 'Cancelled' | 'Expired'; minPlayers: number; maxPlayers: number; winnerCoins: number; secondPlaceCoins: number; participationCoins: number; currentRound: number; expiresAt: string; startedAt?: string | null; completedAt?: string | null; canStart: boolean; players: RoulettePlayer[]; winner?: RoulettePlayer | null; }
export interface RouletteSpinResult { room: RouletteRoom; eliminatedPlayer: RoulettePlayer; }
export interface PendingRouletteIntent { roomId: string; }
