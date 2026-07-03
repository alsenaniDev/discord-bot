export type PageWorkspaceHeroIconName =
  | 'home'
  | 'servers'
  | 'overview'
  | 'settings'
  | 'tickets'
  | 'shield'
  | 'modules'
  | 'subscription'
  | 'logs'
  | 'roles'
  | 'bell'
  | 'admin'
  | 'users'
  | 'guilds'
  | 'external'
  | 'bot'
  | 'refresh'
  | 'check-circle'
  | 'alert-circle'
  | 'clock'
  | 'cloud-off'
  | 'lock';

export type PageWorkspaceHeroBadgeTone = 'success' | 'warning' | 'danger' | 'neutral' | 'info';

export type PageWorkspaceHeroFooterTone = 'success' | 'neutral' | 'warning';

export interface PageWorkspaceHeroStat {
  label: string;
  value: string;
  compact?: boolean;
}

export interface PageWorkspaceHeroAction {
  label: string;
  disabled?: boolean;
  loading?: boolean;
  hidden?: boolean;
  type?: 'button' | 'submit';
}

export interface PageWorkspaceHeroBadge {
  label: string;
  tone?: PageWorkspaceHeroBadgeTone;
}
