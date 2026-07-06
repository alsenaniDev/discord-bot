export interface ActivityGame { id: string; key: string; name: string; description?: string; iconUrl?: string; activityRoute: string; playMode: 'Solo' | 'Multiplayer'; supportsScores: boolean; supportsLeaderboard: boolean; }
export interface LeaderboardEntry { rank: number; userDiscordId: string; username: string; totalPoints: number; gamesPlayed: number; wins: number; losses: number; currentStreak: number; bestStreak: number; }
export interface ActivityContext { guildDiscordId: string; channelDiscordId: string; gamesChannelDiscordId: string; games: ActivityGame[]; leaderboard: LeaderboardEntry[]; }
export interface StartSessionResponse { sessionId: string; gameKey: string; gameName: string; activityRoute: string; expiresAt: string; }
export interface CompleteSessionResponse { sessionId: string; pointsAwarded: number; player: LeaderboardEntry; }
export interface ActivityIdentity { accessToken: string; userId: string; username: string; guildId: string; channelId: string; }
export interface GameWallet { balance: number; }
export interface PowerUpStoreItem { key: string; name: string; description: string; icon: string; isEnabledForGuild: boolean; price: number; maxUsesPerGame: number; ownedQuantity: number; }
export interface PowerUpStore { balance: number; items: PowerUpStoreItem[]; }
export interface PurchasePowerUpResponse { balance: number; powerUpKey: string; ownedQuantity: number; }
export interface RoulettePlayer { userDiscordId: string; username: string; isHost: boolean; isAlive: boolean; position: number; eliminations: number; joinedAt: string; eliminatedAt?: string | null; }
export interface RouletteAction { roundNumber: number; actionType: string; actorUserDiscordId: string; targetUserDiscordId?: string | null; message: string; createdAt: string; }
export interface RouletteSpinInfo { spinnerUserDiscordId: string; spinnerUsername: string; targetUserDiscordId: string; targetUsername: string; resultType: string; createdAt: string; }
export interface RouletteRoom { id: string; guildDiscordId: string; channelDiscordId: string; hostUserDiscordId: string; hostUsername: string; status: 'Waiting' | 'InProgress' | 'Completed' | 'Cancelled' | 'Expired'; minPlayers: number; maxPlayers: number; winnerCoins: number; secondPlaceCoins: number; participationCoins: number; currentRound: number; expiresAt: string; startedAt?: string | null; completedAt?: string | null; canStart: boolean; currentTurnUserDiscordId?: string | null; currentTurnUsername?: string | null; pendingTargetUserDiscordId?: string | null; pendingTargetUsername?: string | null; pendingActionStatus: 'None' | 'WaitingForPowerUp' | 'Resolved' | string; pendingActionExpiresAt?: string | null; lastSpinResult?: RouletteSpinInfo | null; actions: RouletteAction[]; players: RoulettePlayer[]; winner?: RoulettePlayer | null; }
export interface RouletteSpinResult { room: RouletteRoom; eliminatedPlayer?: RoulettePlayer | null; targetPlayer?: RoulettePlayer | null; }
export interface PendingRouletteIntent { roomId: string; }
