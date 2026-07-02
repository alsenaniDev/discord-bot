export type TicketStatus = 'Open' | 'Closed' | 0 | 1;

export interface TicketSummaryReadModel {
  ticketId: string;
  guildId: string;
  ticketNumber: number;
  ownerDiscordId: string;
  ownerUsername?: string | null;
  status: TicketStatus;
  discordChannelId: string;
  createdAt: string;
  closedAt: string | null;
  lastActivityAt: string;
  lastMessagePreview?: string | null;
  messageCount: number;
  staffReplyCount: number;
  failedDeliveryCount: number;
}

export interface PaginatedTicketSummaryReadModel {
  items: TicketSummaryReadModel[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type TicketTimelineEventType =
  | 'TicketCreated'
  | 'MessageSent'
  | 'StaffReplyQueued'
  | 'StaffReplyDelivered'
  | 'StaffReplyFailed'
  | 'StatusChanged'
  | 'ArchivePosted';

export type TicketConversationActorType = 'System' | 'Owner' | 'Staff' | 'Bot';
export type TicketDeliveryStatus = 'None' | 'Queued' | 'Delivered' | 'Failed';

export interface TicketConversationEntryReadModel {
  eventId: string;
  ticketId: string;
  eventType: TicketTimelineEventType | string;
  actorType: TicketConversationActorType | string;
  actorDiscordId?: string | null;
  actorUsername?: string | null;
  content?: string | null;
  isInternal: boolean;
  deliveryStatus: TicketDeliveryStatus | string;
  occurredAt: string;
  createdAt: string;
}

export interface PaginatedTicketConversationReadModel {
  items: TicketConversationEntryReadModel[];
  hasMore: boolean;
  nextCursorOccurredAt?: string | null;
  nextCursorEventId?: string | null;
}

export interface TicketTranscriptMetadataReadModel {
  ticketId: string;
  guildId: string;
  ticketNumber: number;
  ownerDiscordId: string;
  ownerUsername?: string | null;
  status: TicketStatus;
  createdAt: string;
  closedAt: string | null;
  source: string;
  discordArchiveIsDigestOnly: boolean;
}

export interface TicketTranscriptReadModel {
  metadata: TicketTranscriptMetadataReadModel;
  entries: TicketConversationEntryReadModel[];
  hasMore: boolean;
  nextCursorOccurredAt?: string | null;
  nextCursorEventId?: string | null;
}

/** @deprecated Use TicketSummaryReadModel — kept for close response compatibility */
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

export const TICKET_TIMELINE_EVENT_LABELS: Record<string, string> = {
  TicketCreated: 'tickets.timeline.eventTypes.ticketCreated',
  MessageSent: 'tickets.timeline.eventTypes.messageSent',
  StaffReplyQueued: 'tickets.timeline.eventTypes.staffReplyQueued',
  StaffReplyDelivered: 'tickets.timeline.eventTypes.staffReplyDelivered',
  StaffReplyFailed: 'tickets.timeline.eventTypes.staffReplyFailed',
  StatusChanged: 'tickets.timeline.eventTypes.statusChanged',
  ArchivePosted: 'tickets.timeline.eventTypes.archivePosted'
};

export const TICKET_DELIVERY_STATUS_LABELS: Record<string, string> = {
  None: 'tickets.conversation.delivery.none',
  Queued: 'tickets.conversation.delivery.queued',
  Delivered: 'tickets.conversation.delivery.delivered',
  Failed: 'tickets.conversation.delivery.failed'
};

export function ticketTimelineEventLabel(eventType: string): string {
  return TICKET_TIMELINE_EVENT_LABELS[eventType] ?? eventType;
}

export function ticketDeliveryStatusLabel(status: string): string {
  return TICKET_DELIVERY_STATUS_LABELS[status] ?? status;
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
