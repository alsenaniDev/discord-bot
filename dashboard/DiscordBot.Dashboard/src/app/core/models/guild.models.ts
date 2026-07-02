import { OnboardingChecklist } from './onboarding.models';
import { CommandPanelButton } from './command-panel.models';

export interface GuildSummary {
  id: string;
  discordGuildId: string;
  name: string;
  iconUrl?: string;
  isActive: boolean;
  isOwner?: boolean;
  staffRole?: string | null;
}

export interface GuildSettings {
  guildId: string;
  welcomeEnabled: boolean;
  welcomeChannelId?: string;
  welcomeMessage: string;
  autoRoleEnabled: boolean;
  autoRoleId?: string;
  logsEnabled: boolean;
  logChannelId?: string;
  ticketsEnabled: boolean;
  ticketCategoryId?: string;
  ticketArchiveChannelId?: string;
  ticketWelcomeTitle: string;
  ticketWelcomeMessage: string;
  ticketClosedMessage: string;
  ticketClosedFromDashboardMessage: string;
  ticketStaffReplyPrefix: string;
  commandPanelEnabled: boolean;
  commandPanelChannelId?: string;
  commandPanelTitle: string;
  commandPanelDescription: string;
  commandPanelImageUrl?: string;
  commandPanelButtons: CommandPanelButton[];
}

export interface UpdateGuildSettings {
  welcomeEnabled: boolean;
  welcomeChannelId?: string | null;
  welcomeMessage: string;
  autoRoleEnabled: boolean;
  autoRoleId?: string | null;
  logsEnabled: boolean;
  logChannelId?: string | null;
  ticketCategoryId?: string | null;
  ticketArchiveChannelId?: string | null;
  ticketWelcomeTitle: string;
  ticketWelcomeMessage: string;
  ticketClosedMessage: string;
  ticketClosedFromDashboardMessage: string;
  ticketStaffReplyPrefix: string;
  commandPanelEnabled: boolean;
  commandPanelChannelId?: string | null;
  commandPanelTitle: string;
  commandPanelDescription: string;
  commandPanelImageUrl?: string | null;
  commandPanelButtons: CommandPanelButton[];
}

export interface GuildProfile {
  guildId: string;
  discordGuildId: string;
  name: string;
  iconUrl?: string | null;
  displayName?: string | null;
  description?: string | null;
  communityType?: string | null;
  supportMessage?: string | null;
  rulesUrl?: string | null;
  websiteUrl?: string | null;
}

export interface UpdateGuildProfile {
  displayName?: string | null;
  description?: string | null;
  communityType?: string | null;
  supportMessage?: string | null;
  rulesUrl?: string | null;
  websiteUrl?: string | null;
}

export interface ModerationPermissionRole {
  id: string;
  guildId: string;
  roleDiscordId: string;
  roleName?: string | null;
  canWarn: boolean;
  canViewWarnings: boolean;
  canClearMessages: boolean;
  canKick: boolean;
  canViewModerationCases: boolean;
  canViewLogs: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateModerationPermissionRole {
  roleDiscordId: string;
  canWarn: boolean;
  canViewWarnings: boolean;
  canClearMessages: boolean;
  canKick: boolean;
  canViewModerationCases: boolean;
  canViewLogs: boolean;
}

export interface UpdateModerationPermissionRole {
  roleDiscordId: string;
  canWarn: boolean;
  canViewWarnings: boolean;
  canClearMessages: boolean;
  canKick: boolean;
  canViewModerationCases: boolean;
  canViewLogs: boolean;
}

export type ModerationPermissionKey =
  | 'canWarn'
  | 'canViewWarnings'
  | 'canClearMessages'
  | 'canKick'
  | 'canViewModerationCases'
  | 'canViewLogs';

export const MODERATION_PERMISSION_OPTIONS: { value: ModerationPermissionKey; labelKey: string }[] = [
  { value: 'canWarn', labelKey: 'moderationSettings.permissions.canWarn' },
  { value: 'canViewWarnings', labelKey: 'moderationSettings.permissions.canViewWarnings' },
  { value: 'canClearMessages', labelKey: 'moderationSettings.permissions.canClearMessages' },
  { value: 'canKick', labelKey: 'moderationSettings.permissions.canKick' },
  { value: 'canViewModerationCases', labelKey: 'moderationSettings.permissions.canViewModerationCases' },
  { value: 'canViewLogs', labelKey: 'moderationSettings.permissions.canViewLogs' }
];

export interface DiscordChannel {
  discordChannelId: string;
  name: string;
  type: number | string;
  position: number;
}

export interface DiscordRole {
  discordRoleId: string;
  name: string;
  color?: number | null;
  position: number;
  isManaged: boolean;
}

export interface RequestResourceSyncResponse {
  message: string;
  resourcesSyncedAt?: string | null;
}

export interface GuildOverview {
  name: string;
  iconUrl?: string | null;
  isActive: boolean;
  resourcesSyncedAt?: string | null;
  totalChannels: number;
  totalRoles: number;
  totalTickets: number;
  openTickets: number;
  closedTickets: number;
  welcomeEnabled: boolean;
  autoRoleEnabled: boolean;
  logsEnabled: boolean;
  ticketsEnabled: boolean;
  onboarding?: OnboardingChecklist;
}

export function isTextChannel(channel: DiscordChannel): boolean {
  return channel.type === 0 || channel.type === 'Text';
}

export function isCategoryChannel(channel: DiscordChannel): boolean {
  return channel.type === 1 || channel.type === 'Category';
}

export function isAssignableRole(role: DiscordRole): boolean {
  return !role.isManaged;
}

export function channelLabel(channel: DiscordChannel): string {
  return isCategoryChannel(channel) ? channel.name : `#${channel.name}`;
}

export function roleLabel(role: DiscordRole): string {
  return role.name;
}

/** @deprecated Use DiscordChannel */
export type GuildChannel = DiscordChannel;

/** @deprecated Use DiscordRole */
export type GuildRole = DiscordRole;
