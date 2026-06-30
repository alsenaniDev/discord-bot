export interface SubscriptionPlan {
  key: string;
  name: string;
  description: string;
  allowedModules: string[];
  isActive: boolean;
}

export interface GuildSubscription {
  guildId: string;
  planKey: string;
  planName: string;
  planDescription: string;
  allowedModules: string[];
}

export interface UpdateGuildSubscriptionRequest {
  planKey: string;
}
