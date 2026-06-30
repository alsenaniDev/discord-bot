export interface Ticket {
  id: string;
  guildId: string;
  ticketNumber: number;
  ownerDiscordUserId: string;
  channelDiscordId: string;
  status: number | string;
  createdAt: string;
  closedAt: string | null;
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
