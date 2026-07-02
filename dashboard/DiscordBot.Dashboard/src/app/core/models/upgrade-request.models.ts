export type PlanUpgradeRequestStatus = 'Pending' | 'Approved' | 'Rejected';

export interface PlanUpgradeRequest {
  id: string;
  guildId: string;
  requestedPlanKey: string;
  requestedPlanName: string;
  currentPlanKey: string;
  currentPlanName: string;
  requestedByUsername: string;
  durationMonths: number;
  requestedPlanMonthlyPrice: number;
  estimatedTotalPrice: number;
  status: PlanUpgradeRequestStatus;
  adminNote?: string | null;
  createdAt: string;
  reviewedAt?: string | null;
  estimatedExpiresAtIfApprovedToday: string;
}

export interface AdminPlanUpgradeRequest extends PlanUpgradeRequest {
  guildName: string;
  requestedByDiscordUserId: string;
}

export interface CreatePlanUpgradeRequest {
  planKey: string;
  durationMonths: number;
}

export interface ReviewPlanUpgradeRequest {
  adminNote?: string | null;
}
