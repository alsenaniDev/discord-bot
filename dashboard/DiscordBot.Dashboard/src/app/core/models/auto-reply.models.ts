export type AutoReplyMatchMode = 'Contains' | 'Exact';
export type AutoReplyScope = 'AllChannels' | 'TicketChannelsOnly';

export interface AutoReplyRule {
  id: string;
  guildId: string;
  trigger: string;
  response: string;
  matchMode: AutoReplyMatchMode | number | string;
  scope: AutoReplyScope | number | string;
  enabled: boolean;
  priority: number;
  createdAt: string;
}

export interface CreateAutoReplyRule {
  trigger: string;
  response: string;
  matchMode: AutoReplyMatchMode;
  scope: AutoReplyScope;
  enabled: boolean;
  priority: number;
}

export interface UpdateAutoReplyRule extends CreateAutoReplyRule {}

export const AUTO_REPLY_MATCH_MODES: AutoReplyMatchMode[] = ['Contains', 'Exact'];
export const AUTO_REPLY_SCOPES: AutoReplyScope[] = ['AllChannels', 'TicketChannelsOnly'];
