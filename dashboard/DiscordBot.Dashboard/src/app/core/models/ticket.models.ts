export interface Ticket {
  id: string;
  guildId: string;
  ticketNumber: number;
  ownerDiscordUserId: string;
  ownerDisplayName?: string | null;
  channelDiscordId: string;
  channelName?: string | null;
  status: number | string;
  createdAt: string;
  closedAt: string | null;
}

export function isTicketOpen(status: number | string): boolean {
  return status === 0 || status === 'Open';
}

export function ticketStatusLabel(status: number | string): string {
  if (status === 0 || status === 'Open') {
    return 'common.open';
  }
  if (status === 1 || status === 'Closed') {
    return 'common.closed';
  }
  return String(status);
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

export function displayChannelLabel(name?: string | null, id?: string | null): string {
  if (name?.trim()) {
    return `#${name.trim()}`;
  }

  if (id?.trim()) {
    return id.trim();
  }

  return '—';
}
