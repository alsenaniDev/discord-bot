export type LogEventType =
  | 'MemberJoined'
  | 'WelcomeSent'
  | 'AutoRoleAssigned'
  | 'TicketOpened'
  | 'TicketClosed'
  | 'WarningCreated'
  | 'MessagesCleared'
  | 'MemberKicked'
  | 'ModuleChanged'
  | 'SettingsUpdated'
  | 'ResourceSyncCompleted'
  | 'ReactionRoleCreated'
  | 'ReactionRoleAssigned'
  | 'ReactionRoleRemoved'
  | 'ReactionRoleDeleted';

export interface LogEntry {
  id: string;
  type: LogEventType;
  typeLabel: string;
  message: string;
  actorDiscordUserId?: string | null;
  targetDiscordUserId?: string | null;
  channelDiscordId?: string | null;
  actorDisplayName?: string | null;
  targetDisplayName?: string | null;
  channelName?: string | null;
  metadataJson?: string | null;
  createdAt: string;
}

export interface LogFilters {
  type?: LogEventType | '';
  from?: string;
  to?: string;
  search?: string;
  userId?: string;
}

export const LOG_EVENT_TYPE_OPTIONS: { value: LogEventType | ''; labelKey: string }[] = [
  { value: '', labelKey: 'logs.allTypes' },
  { value: 'MemberJoined', labelKey: 'logs.eventTypes.memberJoined' },
  { value: 'WelcomeSent', labelKey: 'logs.eventTypes.welcomeSent' },
  { value: 'AutoRoleAssigned', labelKey: 'logs.eventTypes.autoRoleAssigned' },
  { value: 'TicketOpened', labelKey: 'logs.eventTypes.ticketOpened' },
  { value: 'TicketClosed', labelKey: 'logs.eventTypes.ticketClosed' },
  { value: 'WarningCreated', labelKey: 'logs.eventTypes.warningCreated' },
  { value: 'MessagesCleared', labelKey: 'logs.eventTypes.messagesCleared' },
  { value: 'MemberKicked', labelKey: 'logs.eventTypes.memberKicked' },
  { value: 'ModuleChanged', labelKey: 'logs.eventTypes.moduleChanged' },
  { value: 'SettingsUpdated', labelKey: 'logs.eventTypes.settingsUpdated' },
  { value: 'ResourceSyncCompleted', labelKey: 'logs.eventTypes.resourceSyncCompleted' },
  { value: 'ReactionRoleCreated', labelKey: 'logs.eventTypes.reactionRoleCreated' },
  { value: 'ReactionRoleAssigned', labelKey: 'logs.eventTypes.reactionRoleAssigned' },
  { value: 'ReactionRoleRemoved', labelKey: 'logs.eventTypes.reactionRoleRemoved' },
  { value: 'ReactionRoleDeleted', labelKey: 'logs.eventTypes.reactionRoleDeleted' }
];
