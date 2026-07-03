import { GuildPermissionKey } from '../../core/models/staff.models';

export type StaffPermissionGroupId = 'moderation' | 'tickets' | 'logs' | 'modules' | 'settings';

export interface StaffPermissionGroupDefinition {
  id: StaffPermissionGroupId;
  labelKey: string;
  keys: GuildPermissionKey[];
}

export const STAFF_PERMISSION_GROUPS: StaffPermissionGroupDefinition[] = [
  {
    id: 'moderation',
    labelKey: 'staff.workspace.permissionGroups.moderation',
    keys: [
      'ManageModeration',
      'UseWarn',
      'UseKick',
      'UseBan',
      'UseTimeout',
      'UseClearMessages',
      'ViewWarnings',
      'ViewModerationCases'
    ]
  },
  {
    id: 'tickets',
    labelKey: 'staff.workspace.permissionGroups.tickets',
    keys: ['ViewTickets', 'ReplyToTickets', 'CloseTickets', 'ManageTickets']
  },
  {
    id: 'logs',
    labelKey: 'staff.workspace.permissionGroups.logs',
    keys: ['ViewLogs', 'ClearLogs']
  },
  {
    id: 'modules',
    labelKey: 'staff.workspace.permissionGroups.modules',
    keys: ['ManageModules', 'ManageReactionRoles']
  },
  {
    id: 'settings',
    labelKey: 'staff.workspace.permissionGroups.settings',
    keys: ['AccessDashboard', 'ViewServer', 'ManageSettings', 'ManagePermissionRoles']
  }
];
