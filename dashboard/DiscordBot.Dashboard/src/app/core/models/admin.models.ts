export interface AdminStats {
  totalGuilds: number;
  activeGuilds: number;
  totalUsers: number;
  totalTickets: number;
  openTickets: number;
  planCounts: AdminPlanCount[];
  moduleUsageCounts: AdminModuleUsage[];
}

export interface AdminPlanCount {
  planKey: string;
  planName: string;
  count: number;
}

export interface AdminModuleUsage {
  moduleKey: string;
  moduleName: string;
  enabledGuildCount: number;
}

export interface AdminGuildSummary {
  id: string;
  discordGuildId: string;
  name: string;
  ownerDiscordUserId: string;
  planKey: string;
  planName: string;
  enabledModulesCount: number;
  ticketsCount: number;
  resourcesSyncedAt?: string;
  isActive: boolean;
}

export interface AdminUser {
  id: string;
  discordUserId: string;
  username: string;
  globalName?: string;
  lastLoginAt?: string;
  createdAt: string;
}

export interface UpdateAdminGuildSubscriptionRequest {
  planKey: string;
}

export interface AdminSubscriptionPlan {
  id: string;
  key: string;
  name: string;
  description: string;
  allowedModules: string[];
  isActive: boolean;
  monthlyPrice: number;
  subscriberCount: number;
}

export interface CreateSubscriptionPlanRequest {
  key: string;
  name: string;
  description: string;
  allowedModules: string[];
  monthlyPrice: number;
  isActive: boolean;
}

export interface UpdateSubscriptionPlanRequest {
  name: string;
  description: string;
  allowedModules: string[];
  monthlyPrice: number;
  isActive: boolean;
}

export const PLAN_MODULE_OPTIONS = [
  { value: 'welcome', labelKey: 'subscription.moduleNames.welcome' },
  { value: 'logs', labelKey: 'subscription.moduleNames.logs' },
  { value: 'reaction-roles', labelKey: 'subscription.moduleNames.reaction-roles' },
  { value: 'tickets', labelKey: 'subscription.moduleNames.tickets' },
  { value: 'moderation', labelKey: 'subscription.moduleNames.moderation' },
  { value: 'auto-role', labelKey: 'subscription.moduleNames.auto-role' }
];
