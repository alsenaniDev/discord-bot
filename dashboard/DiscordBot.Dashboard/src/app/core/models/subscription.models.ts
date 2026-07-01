export type SubscriptionPlanStatus = 'Active' | 'Expired' | 'Cancelled';

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
  status: SubscriptionPlanStatus;
  startedAt?: string | null;
  expiresAt?: string | null;
  isExpired: boolean;
}

export interface UpdateGuildSubscriptionRequest {
  planKey: string;
}

export const SUBSCRIPTION_DURATION_OPTIONS = [1, 3, 6, 12] as const;
export type SubscriptionDurationMonths = typeof SUBSCRIPTION_DURATION_OPTIONS[number];

export function isPaidPlan(planKey: string): boolean {
  return planKey !== 'free';
}

export function addMonths(date: Date, months: number): Date {
  const result = new Date(date);
  result.setMonth(result.getMonth() + months);
  return result;
}
