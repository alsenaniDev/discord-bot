export type GuildPermissionKey =
  | 'AccessModeration'
  | 'AccessLogs'
  | 'AccessTickets'
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
}

export const GUILD_PERMISSION_OPTIONS: { value: GuildPermissionKey; labelKey: string }[] = [
  { value: 'AccessModeration', labelKey: 'staff.permissions.accessModeration' },
  { value: 'AccessLogs', labelKey: 'staff.permissions.accessLogs' },
  { value: 'AccessTickets', labelKey: 'staff.permissions.accessTickets' },
  { value: 'ManagePermissionRoles', labelKey: 'staff.permissions.managePermissionRoles' }
];
