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

export type TicketTimelineEventType =
  | 'TicketCreated'
  | 'MessageSent'
  | 'StaffReplyQueued'
  | 'StaffReplyDelivered'
  | 'StaffReplyFailed'
  | 'StatusChanged'
  | 'ArchivePosted';

export interface TicketTimelineEvent {
  id: string;
  ticketId: string;
  eventType: TicketTimelineEventType | string;
  occurredAt: string;
  actorDiscordUserId?: string | null;
  actorDisplayName?: string | null;
  content?: string | null;
  relatedTimelineEventId?: string | null;
  metadataJson?: string | null;
}

export const TICKET_TIMELINE_EVENT_LABELS: Record<string, string> = {
  TicketCreated: 'tickets.timeline.eventTypes.ticketCreated',
  MessageSent: 'tickets.timeline.eventTypes.messageSent',
  StaffReplyQueued: 'tickets.timeline.eventTypes.staffReplyQueued',
  StaffReplyDelivered: 'tickets.timeline.eventTypes.staffReplyDelivered',
  StaffReplyFailed: 'tickets.timeline.eventTypes.staffReplyFailed',
  StatusChanged: 'tickets.timeline.eventTypes.statusChanged',
  ArchivePosted: 'tickets.timeline.eventTypes.archivePosted'
};

export function ticketTimelineEventLabel(eventType: string): string {
  return TICKET_TIMELINE_EVENT_LABELS[eventType] ?? eventType;
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
