export interface OnboardingChecklist {
  botInvited: boolean;
  resourcesSynced: boolean;
  planSelected: boolean;
  modulesEnabled: boolean;
  welcomeConfigured: boolean;
  ticketsConfigured: boolean;
  completedCount: number;
  totalCount: number;
  progressPercent: number;
}

export interface GuildOnboarding {
  guildId: string;
  name: string;
  iconUrl?: string;
  checklist: OnboardingChecklist;
}

export interface OnboardingStatus {
  hasGuilds: boolean;
  botInviteUrl: string;
  dashboardUrl: string;
  guilds: GuildOnboarding[];
}

export interface OnboardingChecklistItem {
  key: string;
  label: string;
  hint: string;
  done: boolean;
}

export function buildChecklistItems(checklist: OnboardingChecklist): OnboardingChecklistItem[] {
  return [
    {
      key: 'botInvited',
      label: 'Bot invited',
      hint: 'Invite the bot to your Discord server.',
      done: checklist.botInvited
    },
    {
      key: 'resourcesSynced',
      label: 'Discord resources synced',
      hint: 'Run `/setup` or `/sync` in Discord after inviting the bot.',
      done: checklist.resourcesSynced
    },
    {
      key: 'planSelected',
      label: 'Plan selected',
      hint: 'Review your subscription plan in the dashboard.',
      done: checklist.planSelected
    },
    {
      key: 'modulesEnabled',
      label: 'Modules enabled',
      hint: 'Turn on the bot features you want to use.',
      done: checklist.modulesEnabled
    },
    {
      key: 'welcomeConfigured',
      label: 'Welcome configured',
      hint: 'Set a welcome channel and message in Settings.',
      done: checklist.welcomeConfigured
    },
    {
      key: 'ticketsConfigured',
      label: 'Tickets configured',
      hint: 'Run `/ticket setup` in Discord or set a ticket category in Settings.',
      done: checklist.ticketsConfigured
    }
  ];
}

export const emptyChecklist = (): OnboardingChecklist => ({
  botInvited: false,
  resourcesSynced: false,
  planSelected: false,
  modulesEnabled: false,
  welcomeConfigured: false,
  ticketsConfigured: false,
  completedCount: 0,
  totalCount: 6,
  progressPercent: 0
});
