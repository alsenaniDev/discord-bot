import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';
import { AdminGuildSummary, AdminSubscriptionPlan } from '../../../core/models/admin.models';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';

@Component({
  selector: 'app-admin-guilds',
  templateUrl: './admin-guilds.component.html',
  styleUrls: ['./admin-guilds.component.css']
})
export class AdminGuildsComponent implements OnInit {
  guilds: AdminGuildSummary[] = [];
  plans: AdminSubscriptionPlan[] = [];
  loading = true;
  error = '';
  savingGuildId: string | null = null;

  constructor(
    private adminService: AdminService,
    private toast: ToastService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.error = '';

    forkJoin({
      guilds: this.adminService.getGuilds(),
      plans: this.adminService.getPlans()
    }).subscribe({
      next: ({ guilds, plans }) => {
        this.guilds = guilds.map(guild => ({
          ...guild,
          planKey: this.normalizePlanKey(guild.planKey)
        }));
        this.plans = plans
          .map(plan => ({ ...plan, key: this.normalizePlanKey(plan.key) }))
          .sort((a, b) => a.monthlyPrice - b.monthlyPrice);
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('errors.loadGuilds'));
        this.loading = false;
      }
    });
  }

  onPlanChange(guild: AdminGuildSummary, planKey: string): void {
    planKey = this.normalizePlanKey(planKey);
    const previousPlanKey = guild.planKey;
    if (!planKey || planKey === previousPlanKey || this.savingGuildId) {
      return;
    }

    this.savingGuildId = guild.id;

    this.adminService.updateGuildSubscription(guild.id, { planKey }).subscribe({
      next: subscription => {
        guild.planKey = this.normalizePlanKey(subscription.planKey);
        guild.planName = subscription.planName;
        this.savingGuildId = null;
        this.toast.success(
          this.translate.instant('admin.guildPlanChanged', {
            guild: guild.name,
            plan: subscription.planName
          })
        );
      },
      error: err => {
        guild.planKey = previousPlanKey;
        this.savingGuildId = null;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('admin.planChangeError')));
      }
    });
  }

  formatLastSync(value?: string): string {
    if (!value) {
      return this.translate.instant('common.never');
    }

    return new Date(value).toLocaleString();
  }

  isSaving(guild: AdminGuildSummary): boolean {
    return this.savingGuildId === guild.id;
  }

  private normalizePlanKey(value?: string | null): string {
    return (value ?? '').trim().toLowerCase();
  }
}
