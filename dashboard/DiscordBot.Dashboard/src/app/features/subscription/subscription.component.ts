import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import { GuildSubscription, SubscriptionPlan } from '../../core/models/subscription.models';
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
  loading = true;
  error = '';
  savingPlanKey: string | null = null;

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
      plans: this.guildService.getPlans()
    }).subscribe({
      next: ({ subscription, plans }) => {
        this.subscription = subscription;
        this.plans = plans;
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('errors.loadSubscription'));
        this.loading = false;
      }
    });
  }

  selectPlan(plan: SubscriptionPlan): void {
    if (this.savingPlanKey || plan.key === this.subscription?.planKey) {
      return;
    }

    this.savingPlanKey = plan.key;

    this.guildService.updateSubscription(this.guildId, { planKey: plan.key }).subscribe({
      next: subscription => {
        this.subscription = subscription;
        this.savingPlanKey = null;
        this.toast.success(
          this.translate.instant('subscription.planChanged', { name: subscription.planName })
        );
      },
      error: err => {
        this.savingPlanKey = null;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('subscription.planChangeError')));
      }
    });
  }

  isCurrentPlan(plan: SubscriptionPlan): boolean {
    return plan.key === this.subscription?.planKey;
  }

  isSaving(plan: SubscriptionPlan): boolean {
    return this.savingPlanKey === plan.key;
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
}
