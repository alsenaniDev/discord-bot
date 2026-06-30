export interface UserProfile {
  id: string;
  discordUserId: string;
  username: string;
  globalName?: string;
  avatarUrl?: string;
  lastLoginAt?: string;
  isAdmin?: boolean;
}

export interface DiscordLoginResponse {
  url: string;
}

export interface ExchangeTokenRequest {
  code: string;
}

export interface ExchangeTokenResponse {
  accessToken: string;
}
