import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  AdminSubscriptionPlan,
  CreateSubscriptionPlanRequest,
  PLAN_MODULE_OPTIONS,
  UpdateSubscriptionPlanRequest
} from '../../../core/models/admin.models';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';

@Component({
  selector: 'app-admin-plans',
  templateUrl: './admin-plans.component.html',
  styleUrls: ['./admin-plans.component.css']
})
export class AdminPlansComponent implements OnInit {
  plans: AdminSubscriptionPlan[] = [];
  loading = true;
  error = '';
  saving = false;
  editingPlanId = '';
  planKey = '';
  planName = '';
  planDescription = '';
  monthlyPrice = 0;
  isActive = true;
  allModules = false;
  selectedModules: Record<string, boolean> = {};
  readonly moduleOptions = PLAN_MODULE_OPTIONS;

  constructor(
    private adminService: AdminService,
    private toast: ToastService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.resetModuleSelection();
    this.loadPlans();
  }

  loadPlans(): void {
    this.loading = true;
    this.error = '';

    this.adminService.getPlans().subscribe({
      next: plans => {
        this.plans = plans;
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('adminPlans.loadError'));
        this.loading = false;
      }
    });
  }

  savePlan(): void {
    const modules = this.buildAllowedModules();
    if (!this.planName.trim() || !this.planDescription.trim() || modules.length === 0 || this.saving) {
      this.toast.error(this.translate.instant('adminPlans.validation.required'));
      return;
    }

    if (!this.editingPlanId && !this.planKey.trim()) {
      this.toast.error(this.translate.instant('adminPlans.validation.required'));
      return;
    }

    this.saving = true;

    const request = this.buildRequest(modules);
    const call = this.editingPlanId
      ? this.adminService.updatePlan(this.editingPlanId, request)
      : this.adminService.createPlan(request as CreateSubscriptionPlanRequest);

    call.subscribe({
      next: plan => {
        if (this.editingPlanId) {
          this.plans = this.plans.map(item => (item.id === plan.id ? plan : item));
        } else {
          this.plans = [...this.plans, plan].sort((a, b) => a.monthlyPrice - b.monthlyPrice);
        }
        this.resetForm();
        this.saving = false;
        this.toast.success(this.translate.instant('adminPlans.saved'));
      },
      error: err => {
        this.saving = false;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('adminPlans.saveError')));
      }
    });
  }

  editPlan(plan: AdminSubscriptionPlan): void {
    this.editingPlanId = plan.id;
    this.planKey = plan.key;
    this.planName = plan.name;
    this.planDescription = plan.description;
    this.monthlyPrice = plan.monthlyPrice;
    this.isActive = plan.isActive;
    this.allModules = plan.allowedModules.includes('*');
    this.selectedModules = this.moduleOptions.reduce<Record<string, boolean>>((acc, option) => {
      acc[option.value] = plan.allowedModules.includes(option.value);
      return acc;
    }, {});
  }

  suspendPlan(plan: AdminSubscriptionPlan): void {
    this.adminService.updatePlan(plan.id, {
      name: plan.name,
      description: plan.description,
      monthlyPrice: plan.monthlyPrice,
      isActive: false,
      allowedModules: plan.allowedModules
    }).subscribe({
      next: updated => {
        this.plans = this.plans.map(item => (item.id === updated.id ? updated : item));
        if (this.editingPlanId === updated.id) {
          this.isActive = false;
        }
        this.toast.success(this.translate.instant('adminPlans.suspended'));
      },
      error: err => {
        this.toast.error(getApiErrorMessage(err, this.translate.instant('adminPlans.saveError')));
      }
    });
  }

  activatePlan(plan: AdminSubscriptionPlan): void {
    this.adminService.updatePlan(plan.id, {
      name: plan.name,
      description: plan.description,
      monthlyPrice: plan.monthlyPrice,
      isActive: true,
      allowedModules: plan.allowedModules
    }).subscribe({
      next: updated => {
        this.plans = this.plans.map(item => (item.id === updated.id ? updated : item));
        if (this.editingPlanId === updated.id) {
          this.isActive = true;
        }
        this.toast.success(this.translate.instant('adminPlans.activated'));
      },
      error: err => {
        this.toast.error(getApiErrorMessage(err, this.translate.instant('adminPlans.saveError')));
      }
    });
  }

  deletePlan(plan: AdminSubscriptionPlan): void {
    if (!window.confirm(this.translate.instant('adminPlans.deleteConfirm', { name: plan.name }))) {
      return;
    }

    this.adminService.deletePlan(plan.id).subscribe({
      next: () => {
        this.plans = this.plans.filter(item => item.id !== plan.id);
        if (this.editingPlanId === plan.id) {
          this.resetForm();
        }
        this.toast.success(this.translate.instant('adminPlans.deleted'));
      },
      error: err => {
        this.toast.error(getApiErrorMessage(err, this.translate.instant('adminPlans.deleteError')));
      }
    });
  }

  cancelEdit(): void {
    this.resetForm();
  }

  onAllModulesChanged(): void {
    if (this.allModules) {
      this.selectedModules = this.moduleOptions.reduce<Record<string, boolean>>((acc, option) => {
        acc[option.value] = false;
        return acc;
      }, {});
    }
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

  get formTitleKey(): string {
    return this.editingPlanId ? 'adminPlans.editTitle' : 'adminPlans.addTitle';
  }

  private buildRequest(modules: string[]): UpdateSubscriptionPlanRequest | CreateSubscriptionPlanRequest {
    const payload = {
      name: this.planName.trim(),
      description: this.planDescription.trim(),
      monthlyPrice: this.monthlyPrice,
      isActive: this.isActive,
      allowedModules: modules
    };

    return this.editingPlanId
      ? payload
      : { ...payload, key: this.planKey.trim().toLowerCase() };
  }

  private buildAllowedModules(): string[] {
    if (this.allModules) {
      return ['*'];
    }

    return this.moduleOptions
      .map(option => option.value)
      .filter(key => this.selectedModules[key]);
  }

  private resetForm(): void {
    this.editingPlanId = '';
    this.planKey = '';
    this.planName = '';
    this.planDescription = '';
    this.monthlyPrice = 0;
    this.isActive = true;
    this.allModules = false;
    this.resetModuleSelection();
  }

  private resetModuleSelection(): void {
    this.selectedModules = this.moduleOptions.reduce<Record<string, boolean>>((acc, option) => {
      acc[option.value] = false;
      return acc;
    }, {});
  }
}
