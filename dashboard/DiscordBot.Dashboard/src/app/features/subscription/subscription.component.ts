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
import { PlanUpgradeRequest } from '../../core/models/upgrade-request.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

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
  loading = true;
  error = '';
  submitting = false;

  selectedPlanKey = '';
  selectedDurationMonths: SubscriptionDurationMonths = 1;
  readonly durationOptions = SUBSCRIPTION_DURATION_OPTIONS;

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
      subscription: this.guildService.getSubscription(this.guildId),
      plans: this.guildService.getPlans(),
      upgradeRequests: this.guildService.getUpgradeRequests(this.guildId)
    }).subscribe({
      next: ({ subscription, plans, upgradeRequests }) => {
        this.subscription = subscription;
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

  requestUpgrade(): void {
    const plan = this.selectedPlan;
    if (!plan || this.submitting || this.hasPendingRequest) {
      return;
    }

    this.submitting = true;

    this.guildService.createUpgradeRequest(this.guildId, {
      planKey: plan.key,
      durationMonths: this.selectedDurationMonths
    }).subscribe({
      next: request => {
        this.upgradeRequests = [request, ...this.upgradeRequests.filter(r => r.id !== request.id)];
        this.submitting = false;
        this.toast.success(
          this.translate.instant('subscription.upgradeRequested', {
            name: plan.name,
            months: this.selectedDurationMonths
          })
        );
      },
      error: err => {
        this.submitting = false;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('subscription.upgradeRequestError')));
      }
    });
  }

  get selectedPlan(): SubscriptionPlan | undefined {
    return this.plans.find(plan => plan.key === this.selectedPlanKey);
  }

  get pendingRequest(): PlanUpgradeRequest | undefined {
    return this.upgradeRequests.find(r => r.status === 'Pending');
  }

  get hasPendingRequest(): boolean {
    return !!this.pendingRequest;
  }

  get estimatedExpiryDate(): Date {
    return addMonths(new Date(), this.selectedDurationMonths);
  }

  isCurrentPlan(plan: SubscriptionPlan): boolean {
    return plan.key === this.subscription?.planKey && this.subscription?.status === 'Active';
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

  subscriptionStatusLabel(status: GuildSubscription['status']): string {
    return this.translate.instant(`subscription.subscriptionStatus.${status.toLowerCase()}`);
  }

  durationLabel(months: number): string {
    return this.translate.instant('subscription.durationMonths', { count: months });
  }
}
