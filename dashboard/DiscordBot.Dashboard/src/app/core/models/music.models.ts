export interface GuildMusicSettings {
  guildId: string;
  isEnabled: boolean;
  djRoleDiscordId?: string | null;
  maxQueueSize: number;
  maxTrackDurationSeconds: number;
  defaultVolume: number;
  allowEveryoneToQueue: boolean;
  createdAtUtc?: string | null;
  updatedAtUtc?: string | null;
}

export type UpdateGuildMusicSettings = Omit<GuildMusicSettings, 'guildId' | 'createdAtUtc' | 'updatedAtUtc'>;
