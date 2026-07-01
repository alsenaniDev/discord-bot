export interface CommandPanelButton {
  id: string;
  action: string;
  label: string;
  style: 'Primary' | 'Secondary' | 'Success' | 'Danger';
  enabled: boolean;
  order: number;
}

export const COMMAND_PANEL_ACTIONS: { value: string; labelKey: string }[] = [
  { value: 'ticket_open', labelKey: 'settings.panel.actions.ticketOpen' },
  { value: 'ticket_help', labelKey: 'settings.panel.actions.ticketHelp' },
  { value: 'ping', labelKey: 'settings.panel.actions.ping' },
  { value: 'server_info', labelKey: 'settings.panel.actions.serverInfo' },
  { value: 'moderation_help', labelKey: 'settings.panel.actions.moderationHelp' },
  { value: 'reaction_roles_help', labelKey: 'settings.panel.actions.reactionRolesHelp' },
  { value: 'platform_help', labelKey: 'settings.panel.actions.platformHelp' }
];

export const COMMAND_PANEL_STYLES: { value: CommandPanelButton['style']; labelKey: string }[] = [
  { value: 'Primary', labelKey: 'settings.panel.styles.primary' },
  { value: 'Secondary', labelKey: 'settings.panel.styles.secondary' },
  { value: 'Success', labelKey: 'settings.panel.styles.success' },
  { value: 'Danger', labelKey: 'settings.panel.styles.danger' }
];

export const DEFAULT_COMMAND_PANEL_BUTTONS: CommandPanelButton[] = [
  {
    id: 'ticket-open',
    action: 'ticket_open',
    label: 'Create Ticket',
    style: 'Success',
    enabled: true,
    order: 0
  },
  {
    id: 'ticket-help',
    action: 'ticket_help',
    label: 'Ticket Help',
    style: 'Secondary',
    enabled: true,
    order: 1
  }
];
