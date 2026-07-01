import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';
import { AdminPlanUpgradeRequest } from '../../../core/models/upgrade-request.models';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';

@Component({
  selector: 'app-admin-upgrade-requests',
  templateUrl: './admin-upgrade-requests.component.html',
  styleUrls: ['./admin-upgrade-requests.component.css']
})
export class AdminUpgradeRequestsComponent implements OnInit {
  requests: AdminPlanUpgradeRequest[] = [];
  loading = true;
  error = '';
  processingId: string | null = null;
  notes: Record<string, string> = {};

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

    this.adminService.getUpgradeRequests().subscribe({
      next: requests => {
        this.requests = requests;
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('admin.upgradeRequestsError'));
        this.loading = false;
      }
    });
  }

  approve(request: AdminPlanUpgradeRequest): void {
    this.review(request, 'approve');
  }

  reject(request: AdminPlanUpgradeRequest): void {
    this.review(request, 'reject');
  }

  private review(request: AdminPlanUpgradeRequest, action: 'approve' | 'reject'): void {
    if (this.processingId) {
      return;
    }

    this.processingId = request.id;
    const body = { adminNote: this.notes[request.id]?.trim() || null };
    const call =
      action === 'approve'
        ? this.adminService.approveUpgradeRequest(request.id, body)
        : this.adminService.rejectUpgradeRequest(request.id, body);

    call.subscribe({
      next: updated => {
        this.requests = this.requests.map(item => (item.id === updated.id ? updated : item));
        this.processingId = null;
        this.toast.success(
          this.translate.instant(
            action === 'approve' ? 'admin.upgradeApproved' : 'admin.upgradeRejected',
            { guild: updated.guildName, plan: updated.requestedPlanName }
          )
        );
      },
      error: err => {
        this.processingId = null;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('admin.upgradeReviewError')));
      }
    });
  }

  isProcessing(request: AdminPlanUpgradeRequest): boolean {
    return this.processingId === request.id;
  }

  statusLabel(status: AdminPlanUpgradeRequest['status']): string {
    return this.translate.instant(`subscription.requestStatus.${status.toLowerCase()}`);
  }

  durationLabel(months: number): string {
    return this.translate.instant('subscription.durationMonths', { count: months });
  }
}
