export type SubscriptionChangeType = 'Upgrade' | 'Renewal';

export type PlanUpgradeRequestStatus =
  | 'Requested'
  | 'PendingPayment'
  | 'PaymentSubmitted'
  | 'UnderReview'
  | 'Approved'
  | 'Activated'
  | 'Rejected'
  | 'Cancelled'
  | 'Expired';

export const ACTIVE_UPGRADE_REQUEST_STATUSES: PlanUpgradeRequestStatus[] = [
  'Requested',
  'PendingPayment',
  'PaymentSubmitted',
  'UnderReview',
  'Approved'
];

export const REVIEWABLE_UPGRADE_REQUEST_STATUSES: PlanUpgradeRequestStatus[] = [
  'PendingPayment',
  'PaymentSubmitted',
  'UnderReview'
];

export function isActiveUpgradeRequestStatus(status: PlanUpgradeRequestStatus): boolean {
  return ACTIVE_UPGRADE_REQUEST_STATUSES.includes(status);
}

export function isReviewableUpgradeRequestStatus(status: PlanUpgradeRequestStatus): boolean {
  return REVIEWABLE_UPGRADE_REQUEST_STATUSES.includes(status);
}

export interface PlanUpgradeRequest {
  id: string;
  guildId: string;
  changeType: SubscriptionChangeType;
  requestedPlanKey: string;
  requestedPlanName: string;
  currentPlanKey: string;
  currentPlanName: string;
  requestedByUsername: string;
  durationMonths: number;
  requestedPlanMonthlyPrice: number;
  estimatedTotalPrice: number;
  status: PlanUpgradeRequestStatus;
  paymentReference?: string | null;
  adminNote?: string | null;
  createdAt: string;
  reviewedAt?: string | null;
  paymentSubmittedAt?: string | null;
  requestExpiresAt?: string | null;
  estimatedExpiresAtIfApprovedToday: string;
}

export interface AdminPlanUpgradeRequest extends PlanUpgradeRequest {
  guildName: string;
  requestedByDiscordUserId: string;
  adminOverrideReason?: string | null;
}

export interface CreatePlanUpgradeRequest {
  planKey: string;
  durationMonths: number;
  changeType?: SubscriptionChangeType;
}

export interface SubmitPaymentReferenceRequest {
  paymentReference: string;
}

export interface GuildSubscriptionStatus {
  subscription: import('./subscription.models').GuildSubscription;
  currentChange: PlanUpgradeRequest | null;
}

export interface ReviewPlanUpgradeRequest {
  adminNote?: string | null;
  adminOverrideReason?: string | null;
}
