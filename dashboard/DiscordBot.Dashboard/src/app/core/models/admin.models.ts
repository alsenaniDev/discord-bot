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
