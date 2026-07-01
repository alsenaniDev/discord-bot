export interface GuildMember {
  discordUserId: string;
  username: string;
  globalName?: string | null;
  nickname?: string | null;
  displayName: string;
}
