export type ModuleCategory =
  | 'community'
  | 'support'
  | 'moderation'
  | 'automation'
  | 'engagement'
  | 'logs';

export type ModuleIconName =
  | 'overview'
  | 'tickets'
  | 'shield'
  | 'logs'
  | 'roles'
  | 'bell';

export type ModuleIconTone = 'green' | 'blue' | 'purple' | 'orange' | 'teal' | 'pink';

export interface ModuleUiMeta {
  icon: ModuleIconName;
  iconTone: ModuleIconTone;
  category: ModuleCategory;
  route: string[];
}

export const MODULE_CATEGORY_ORDER: ModuleCategory[] = [
  'community',
  'support',
  'moderation',
  'automation',
  'engagement',
  'logs'
];

export const MODULE_UI_CONFIG: Record<string, ModuleUiMeta> = {
  welcome: { icon: 'bell', iconTone: 'green', category: 'community', route: ['settings'] },
  tickets: { icon: 'tickets', iconTone: 'blue', category: 'community', route: ['tickets'] },
  moderation: { icon: 'shield', iconTone: 'purple', category: 'moderation', route: ['moderation'] },
  logs: { icon: 'logs', iconTone: 'orange', category: 'logs', route: ['logs'] },
  'auto-role': { icon: 'roles', iconTone: 'teal', category: 'automation', route: ['settings'] },
  'reaction-roles': { icon: 'roles', iconTone: 'pink', category: 'engagement', route: ['reaction-roles'] }
};

export function getModuleUiMeta(key: string): ModuleUiMeta | undefined {
  return MODULE_UI_CONFIG[key];
}
