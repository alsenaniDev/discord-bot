import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import {
  GuildSubscription,
  SubscriptionDurationMonths,
  SubscriptionPlan,
  SUBSCRIPTION_DURATION_OPTIONS,
  addMonths,
  isPaidPlan
} from '../../core/models/subscription.models';
import {
  PlanUpgradeRequest,
  SubscriptionChangeType
} from '../../core/models/upgrade-request.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import { MANUAL_BILLING_CONFIG, buildPaymentReferenceHint } from './config/manual-billing.config';
import {
  PageWorkspaceHeroAction,
  PageWorkspaceHeroBadge,
  PageWorkspaceHeroStat
} from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';

type SubscriptionHeroCta = 'payment' | 'renew' | 'request' | 'modules' | 'none';

@Component({
  selector: 'app-subscription',
  templateUrl: './subscription.component.html',
  styleUrls: ['./subscription.component.css']
})
export class SubscriptionComponent implements OnInit {
  guildId = '';
  subscription: GuildSubscription | null = null;
  plans: SubscriptionPlan[] = [];
  upgradeRequests: PlanUpgradeRequest[] = [];
  currentChange: PlanUpgradeRequest | null = null;
  loading = true;
  error = '';
  submitting = false;
  submittingPayment = false;
  cancelling = false;
  showCancelDialog = false;
  showConfirmDialog = false;

  selectedPlanKey = '';
  selectedDurationMonths: SubscriptionDurationMonths = 1;
  pendingChangeType: SubscriptionChangeType = 'Upgrade';
  paymentReference = '';

  readonly durationOptions = SUBSCRIPTION_DURATION_OPTIONS;
  readonly billingConfig = MANUAL_BILLING_CONFIG;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private guildService: GuildService,
    private guildContext: GuildContextService,
    private toast: ToastService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.guildId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.guildId) {
      this.router.navigate(['/servers']);
      return;
    }

    this.guildContext.ensureGuild(this.guildId, this.guildService);
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.error = '';

    forkJoin({
      status: this.guildService.getSubscriptionStatus(this.guildId),
      plans: this.guildService.getPlans(),
      upgradeRequests: this.guildService.getUpgradeRequests(this.guildId)
    }).subscribe({
      next: ({ status, plans, upgradeRequests }) => {
        this.subscription = status.subscription;
        this.currentChange = status.currentChange;
        this.plans = plans.filter(plan => isPaidPlan(plan.key));
        this.upgradeRequests = upgradeRequests;
        if (!this.selectedPlanKey && this.plans.length > 0) {
          this.selectedPlanKey = this.plans[0].key;
        }
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('errors.loadSubscription'));
        this.loading = false;
      }
    });
  }

  openConfirmDialog(): void {
    if (!this.selectedPlan || this.hasActiveChange) {
      return;
    }

    this.showConfirmDialog = true;
  }

  closeConfirmDialog(): void {
    if (this.submitting) {
      return;
    }

    this.showConfirmDialog = false;
  }

  requestChange(): void {
    const plan = this.selectedPlan;
    if (!plan || this.submitting || this.hasActiveChange) {
      return;
    }

    this.submitting = true;

    this.guildService.createUpgradeRequest(this.guildId, {
      planKey: plan.key,
      durationMonths: this.selectedDurationMonths,
      changeType: this.pendingChangeType
    }).subscribe({
      next: request => {
        this.currentChange = request;
        this.upgradeRequests = [request, ...this.upgradeRequests.filter(r => r.id !== request.id)];
        this.paymentReference = '';
        this.submitting = false;
        this.showConfirmDialog = false;
        this.toast.success(
          this.translate.instant(
            request.changeType === 'Renewal'
              ? 'subscription.renewalRequested'
              : 'subscription.upgradeRequested',
            { name: plan.name, months: this.selectedDurationMonths }
          )
        );
        this.scrollToId('subscription-change-flow');
      },
      error: err => {
        this.submitting = false;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('subscription.changeRequestError')));
      }
    });
  }

  submitPaymentReference(): void {
    if (!this.currentChange || this.submittingPayment || !this.paymentReference.trim()) {
      return;
    }

    this.submittingPayment = true;

    this.guildService.submitPaymentReference(this.guildId, this.currentChange.id, {
      paymentReference: this.paymentReference.trim()
    }).subscribe({
      next: request => {
        this.currentChange = request;
        this.upgradeRequests = this.upgradeRequests.map(r => (r.id === request.id ? request : r));
        this.paymentReference = '';
        this.submittingPayment = false;
        this.toast.success(this.translate.instant('subscription.paymentSubmittedSuccess'));
      },
      error: err => {
        this.submittingPayment = false;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('subscription.paymentSubmittedError')));
      }
    });
  }

  openCancelDialog(): void {
    this.showCancelDialog = true;
  }

  closeCancelDialog(): void {
    if (this.cancelling) {
      return;
    }

    this.showCancelDialog = false;
  }

  confirmCancelChange(): void {
    if (!this.currentChange || this.cancelling) {
      return;
    }

    this.cancelling = true;

    this.guildService.cancelUpgradeRequest(this.guildId, this.currentChange.id).subscribe({
      next: request => {
        this.currentChange = null;
        this.upgradeRequests = this.upgradeRequests.map(r => (r.id === request.id ? request : r));
        this.cancelling = false;
        this.showCancelDialog = false;
        this.toast.success(this.translate.instant('subscription.changeCancelledSuccess'));
      },
      error: err => {
        this.cancelling = false;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('subscription.changeCancelledError')));
      }
    });
  }

  onHeroCta(action: SubscriptionHeroCta): void {
    switch (action) {
      case 'payment':
        this.scrollToId('subscription-payment-section');
        break;
      case 'renew':
        this.startRenewal();
        break;
      case 'request':
        this.scrollToId('subscription-request');
        break;
      case 'modules':
        this.router.navigate(['/guilds', this.guildId, 'modules']);
        break;
      default:
        break;
    }
  }

  onStickyCta(): void {
    const action = this.heroCta;
    if (action === 'none') {
      return;
    }

    this.onHeroCta(action);
  }

  startRenewal(): void {
    if (!this.subscription || this.hasActiveChange) {
      return;
    }

    this.selectedPlanKey = this.subscription.planKey;
    this.pendingChangeType = 'Renewal';
    this.selectedDurationMonths = 1;
    this.scrollToId('subscription-request');
  }

  startNewRequest(): void {
    this.scrollToId('subscription-request');
  }

  onPlanSelectionChange(): void {
    if (this.subscription && this.selectedPlanKey === this.subscription.planKey) {
      this.pendingChangeType = 'Renewal';
    } else {
      this.pendingChangeType = 'Upgrade';
    }
  }

  get selectedPlan(): SubscriptionPlan | undefined {
    return this.plans.find(plan => plan.key === this.selectedPlanKey);
  }

  get hasActiveChange(): boolean {
    return !!this.currentChange;
  }

  get canRenew(): boolean {
    return (
      !!this.subscription &&
      isPaidPlan(this.subscription.planKey) &&
      this.subscription.status === 'Active' &&
      !this.hasActiveChange
    );
  }

  get recentTerminalRequest(): PlanUpgradeRequest | null {
    if (this.currentChange || this.upgradeRequests.length === 0) {
      return null;
    }

    const latest = this.upgradeRequests[0];
    if (['Rejected', 'Cancelled', 'Expired'].includes(latest.status)) {
      return latest;
    }

    return null;
  }

  get paymentReferenceHint(): string {
    return buildPaymentReferenceHint(this.guildId);
  }

  get paymentAmount(): number {
    return this.currentChange?.estimatedTotalPrice ?? 0;
  }

  get estimatedExpiryDate(): Date {
    const base = this.subscription?.expiresAt && !this.subscription.isExpired
      ? new Date(this.subscription.expiresAt)
      : new Date();
    return addMonths(base, this.selectedDurationMonths);
  }

  get estimatedTotalPrice(): number {
    return (this.selectedPlan?.monthlyPrice ?? 0) * this.selectedDurationMonths;
  }

  get stepperIndex(): number {
    const status = this.currentChange?.status;
    switch (status) {
      case 'Requested':
        return 1;
      case 'PendingPayment':
        return 2;
      case 'PaymentSubmitted':
        return 3;
      case 'UnderReview':
        return 4;
      case 'Approved':
        return 5;
      default:
        return 0;
    }
  }

  get heroStatusBadgeKey(): string {
    if (!this.subscription) {
      return 'subscription.hero.badges.unknown';
    }

    if (this.currentChange) {
      return `subscription.hero.badges.request.${this.currentChange.status.toLowerCase()}`;
    }

    if (this.subscription.isExpired) {
      return 'subscription.hero.badges.expired';
    }

    return `subscription.subscriptionStatus.${this.subscription.status.toLowerCase()}`;
  }

  get heroStatusBadgeTone(): 'success' | 'warning' | 'danger' | 'neutral' | 'info' {
    if (this.currentChange?.status === 'PendingPayment') {
      return 'warning';
    }

    if (this.currentChange) {
      return 'info';
    }

    if (this.subscription?.isExpired) {
      return 'danger';
    }

    if (this.subscription?.status === 'Active') {
      return 'success';
    }

    return 'neutral';
  }

  get heroTitleKey(): string {
    if (this.currentChange) {
      return 'subscription.hero.titles.activeChange';
    }

    if (this.subscription?.isExpired) {
      return 'subscription.hero.titles.expired';
    }

    return 'subscription.hero.titles.plan';
  }

  get heroDescriptionKey(): string {
    if (this.currentChange) {
      return 'subscription.hero.descriptions.activeChange';
    }

    if (this.subscription?.isExpired) {
      return 'subscription.hero.descriptions.expired';
    }

    if (this.hasActiveChange) {
      return 'subscription.hero.descriptions.waiting';
    }

    return 'subscription.hero.descriptions.default';
  }

  get heroTitleParams(): Record<string, string> {
    return {
      plan: this.subscription?.planName ?? '',
      changePlan: this.currentChange?.requestedPlanName ?? ''
    };
  }

  get heroDescriptionParams(): Record<string, string> {
    return {
      plan: this.subscription?.planName ?? '',
      changePlan: this.currentChange?.requestedPlanName ?? '',
      expiry: this.formatDate(this.subscription?.expiresAt),
      days: this.billingConfig.reviewSlaDays
    };
  }

  get confirmDialogParams(): Record<string, string> {
    return {
      plan: this.selectedPlan?.name ?? '',
      duration: this.durationLabel(this.selectedDurationMonths),
      total: new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(this.estimatedTotalPrice)
    };
  }

  get heroExpiryLabel(): string {
    if (!this.subscription?.expiresAt) {
      return '';
    }

    return this.translate.instant('subscription.hero.expiryLabel', {
      date: this.formatDate(this.subscription.expiresAt)
    });
  }

  get heroCta(): SubscriptionHeroCta {
    if (this.currentChange?.status === 'PendingPayment') {
      return 'payment';
    }

    if (this.hasActiveChange) {
      return 'none';
    }

    if (this.subscription?.isExpired || this.canRenew) {
      return 'renew';
    }

    if (this.plans.length > 0) {
      return 'request';
    }

    return 'none';
  }

  get heroCtaLabelKey(): string {
    switch (this.heroCta) {
      case 'payment':
        return 'subscription.hero.cta.payment';
      case 'renew':
        return 'subscription.hero.cta.renew';
      case 'request':
        return 'subscription.hero.cta.request';
      case 'modules':
        return 'subscription.hero.cta.modules';
      default:
        return '';
    }
  }

  get stickyCtaLabelKey(): string {
    return this.heroCta === 'none' ? '' : this.heroCtaLabelKey;
  }

  get workspaceHeroTitle(): string {
    return this.translate.instant(this.heroTitleKey, this.heroTitleParams);
  }

  get workspaceHeroDescription(): string {
    return this.translate.instant(this.heroDescriptionKey, this.heroDescriptionParams);
  }

  get workspaceHeroBadge(): PageWorkspaceHeroBadge {
    return {
      label: this.translate.instant(this.heroStatusBadgeKey),
      tone: this.heroStatusBadgeTone
    };
  }

  get workspaceHeroStats(): PageWorkspaceHeroStat[] {
    const pendingRequests = this.upgradeRequests.filter(request =>
      ['Requested', 'PendingPayment', 'PaymentSubmitted', 'UnderReview'].includes(request.status)
    ).length;
    const completedPayments = this.upgradeRequests.filter(request =>
      ['Approved'].includes(request.status)
    ).length;

    return [
      {
        label: this.translate.instant('workspaceHero.subscription.stats.plan'),
        value: this.subscription?.planName ?? this.translate.instant('common.emptyValue'),
        compact: true
      },
      {
        label: this.translate.instant('workspaceHero.subscription.stats.expires'),
        value: this.subscription?.expiresAt
          ? this.formatDate(this.subscription.expiresAt)
          : this.translate.instant('common.emptyValue'),
        compact: true
      },
      {
        label: this.translate.instant('workspaceHero.subscription.stats.requests'),
        value: String(pendingRequests)
      },
      {
        label: this.translate.instant('workspaceHero.subscription.stats.payments'),
        value: String(completedPayments)
      }
    ];
  }

  get workspaceHeroFooter(): string {
    return this.translate.instant('workspaceHero.subscription.footer');
  }

  get workspaceHeroPrimaryAction(): PageWorkspaceHeroAction | null {
    if (this.heroCta === 'none') {
      return null;
    }

    return {
      label: this.translate.instant(this.heroCtaLabelKey)
    };
  }

  get showPaymentInstructions(): boolean {
    return this.currentChange?.status === 'PendingPayment';
  }

  terminalStateIcon(status: PlanUpgradeRequest['status']): string {
    switch (status) {
      case 'Rejected':
        return 'x';
      case 'Cancelled':
        return 'x';
      case 'Expired':
        return 'clock';
      default:
        return 'alert-circle';
    }
  }

  terminalStateTone(status: PlanUpgradeRequest['status']): string {
    switch (status) {
      case 'Rejected':
        return 'danger';
      case 'Cancelled':
        return 'neutral';
      case 'Expired':
        return 'warning';
      default:
        return 'neutral';
    }
  }

  terminalStateTitle(status: PlanUpgradeRequest['status']): string {
    return this.translate.instant(`subscription.states.${status.toLowerCase()}.title`);
  }

  terminalStateBody(status: PlanUpgradeRequest['status']): string {
    return this.translate.instant(`subscription.states.${status.toLowerCase()}.body`);
  }

  formatModules(modules: string[]): string {
    if (modules.includes('*')) {
      return this.translate.instant('subscription.allModules');
    }

    return modules
      .map(key => {
        const labelKey = `subscription.moduleNames.${key}`;
        const translated = this.translate.instant(labelKey);
        return translated === labelKey ? key : translated;
      })
      .join(', ');
  }

  statusLabel(status: PlanUpgradeRequest['status']): string {
    return this.translate.instant(`subscription.requestStatus.${status.toLowerCase()}`);
  }

  changeTypeLabel(changeType: SubscriptionChangeType): string {
    return this.translate.instant(`subscription.changeType.${changeType.toLowerCase()}`);
  }

  durationLabel(months: number): string {
    return this.translate.instant('subscription.durationMonths', { count: months });
  }

  isCurrentPlan(plan: SubscriptionPlan): boolean {
    return plan.key === this.subscription?.planKey && this.subscription?.status === 'Active';
  }

  private formatDate(value?: string | null): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return new Intl.DateTimeFormat(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(date);
  }

  private scrollToId(id: string): void {
    requestAnimationFrame(() => {
      document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }
}
