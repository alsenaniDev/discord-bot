export type GuildPermissionKey =
  | 'AccessDashboard'
  | 'ViewServer'
  | 'ManageSettings'
  | 'ManageModules'
  | 'ViewTickets'
  | 'ReplyToTickets'
  | 'CloseTickets'
  | 'ManageTickets'
  | 'ManageModeration'
  | 'UseWarn'
  | 'UseKick'
  | 'UseBan'
  | 'UseTimeout'
  | 'UseClearMessages'
  | 'ViewWarnings'
  | 'ViewModerationCases'
  | 'ViewLogs'
  | 'ClearLogs'
  | 'ManageReactionRoles'
  | 'ManagePermissionRoles';

export interface GuildPermissionRole {
  id: string;
  guildId: string;
  name: string;
  discordRoleId: string;
  discordRoleName?: string | null;
  permissionKeys: string[];
  createdAt: string;
}

export interface CreateGuildPermissionRoleRequest {
  name: string;
  discordRoleId: string;
  permissionKeys: GuildPermissionKey[];
}

export interface UpdateGuildPermissionRoleRequest {
  name: string;
  discordRoleId: string;
  permissionKeys: GuildPermissionKey[];
}

export interface GuildAccess {
  isOwner: boolean;
  isPlatformAdmin: boolean;
  staffRole?: string | null;
  canWarn: boolean;
  canKick: boolean;
  canTimeout: boolean;
  canClearMessages: boolean;
  canManageSettings: boolean;
  canManageModules: boolean;
  canManageSubscription: boolean;
  canManageStaff: boolean;
  canAccessModeration: boolean;
  canAccessLogs: boolean;
  canAccessTickets: boolean;
  canAccessOverview: boolean;
  canClearLogs?: boolean;
}

export const GUILD_PERMISSION_OPTIONS: { value: GuildPermissionKey; labelKey: string }[] = [
  { value: 'AccessDashboard', labelKey: 'staff.permissions.accessDashboard' },
  { value: 'ViewServer', labelKey: 'staff.permissions.viewServer' },
  { value: 'ManageSettings', labelKey: 'staff.permissions.manageSettings' },
  { value: 'ManageModules', labelKey: 'staff.permissions.manageModules' },
  { value: 'ViewTickets', labelKey: 'staff.permissions.viewTickets' },
  { value: 'ReplyToTickets', labelKey: 'staff.permissions.replyToTickets' },
  { value: 'CloseTickets', labelKey: 'staff.permissions.closeTickets' },
  { value: 'ManageTickets', labelKey: 'staff.permissions.manageTickets' },
  { value: 'ManageModeration', labelKey: 'staff.permissions.manageModeration' },
  { value: 'UseWarn', labelKey: 'staff.permissions.useWarn' },
  { value: 'UseKick', labelKey: 'staff.permissions.useKick' },
  { value: 'UseBan', labelKey: 'staff.permissions.useBan' },
  { value: 'UseTimeout', labelKey: 'staff.permissions.useTimeout' },
  { value: 'UseClearMessages', labelKey: 'staff.permissions.useClearMessages' },
  { value: 'ViewWarnings', labelKey: 'staff.permissions.viewWarnings' },
  { value: 'ViewModerationCases', labelKey: 'staff.permissions.viewModerationCases' },
  { value: 'ViewLogs', labelKey: 'staff.permissions.viewLogs' },
  { value: 'ClearLogs', labelKey: 'staff.permissions.clearLogs' },
  { value: 'ManageReactionRoles', labelKey: 'staff.permissions.manageReactionRoles' },
  { value: 'ManagePermissionRoles', labelKey: 'staff.permissions.managePermissionRoles' }
];

export const MODERATION_BOT_PERMISSION_KEYS: GuildPermissionKey[] = [
  'UseWarn',
  'UseKick',
  'UseBan',
  'UseTimeout',
  'UseClearMessages',
  'ViewWarnings',
  'ViewModerationCases',
  'ViewLogs'
];

export const LEGACY_GUILD_PERMISSION_ALIASES: Record<string, GuildPermissionKey> = {
  AccessModeration: 'ManageModeration',
  AccessLogs: 'ViewLogs',
  AccessTickets: 'ViewTickets',
  Warn: 'UseWarn',
  Kick: 'UseKick',
  Timeout: 'UseTimeout',
  ClearMessages: 'UseClearMessages'
};

export function normalizePermissionKeys(keys: string[] | undefined | null): GuildPermissionKey[] {
  const normalized = new Set<GuildPermissionKey>();

  for (const key of keys ?? []) {
    const mapped = (LEGACY_GUILD_PERMISSION_ALIASES[key] ?? key) as GuildPermissionKey;
    if (GUILD_PERMISSION_OPTIONS.some(option => option.value === mapped)) {
      normalized.add(mapped);
    }
  }

  return [...normalized];
}

export function hasModerationBotPermissions(keys: string[] | undefined | null): boolean {
  const normalized = normalizePermissionKeys(keys);
  return MODERATION_BOT_PERMISSION_KEYS.some(key => normalized.includes(key));
}
