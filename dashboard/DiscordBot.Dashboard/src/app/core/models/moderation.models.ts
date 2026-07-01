export interface Warning {
  id: string;
  targetDiscordUserId: string;
  targetDisplayName?: string | null;
  moderatorDiscordUserId: string;
  moderatorDisplayName?: string | null;
  reason: string;
  createdAt: string;
}

export interface ModerationCase {
  id: string;
  type: number | string;
  targetDiscordUserId?: string | null;
  targetDisplayName?: string | null;
  moderatorDiscordUserId: string;
  moderatorDisplayName?: string | null;
  reason?: string | null;
  messageCount?: number | null;
  channelDiscordId?: string | null;
  channelName?: string | null;
  createdAt: string;
}

export interface ModerationFilters {
  targetUserId?: string;
  type?: string;
  from?: string;
  to?: string;
}

export function moderationCaseTypeLabel(type: number | string): string {
  if (type === 0 || type === 'Warn') return 'moderation.warn';
  if (type === 1 || type === 'Kick') return 'moderation.kick';
  if (type === 2 || type === 'Clear') return 'moderation.clear';
  return String(type);
}

export function displayMemberLabel(name?: string | null, id?: string | null): string {
  if (name?.trim()) {
    return name.trim();
  }

  if (id?.trim()) {
    return id.trim();
  }

  return '—';
}
