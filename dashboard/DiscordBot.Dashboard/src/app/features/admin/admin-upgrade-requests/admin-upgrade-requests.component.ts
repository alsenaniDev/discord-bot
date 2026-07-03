import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  AdminPlanUpgradeRequest,
  PlanUpgradeRequestStatus,
  SubscriptionChangeType,
  isReviewableUpgradeRequestStatus
} from '../../../core/models/upgrade-request.models';
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

  filterStatus: PlanUpgradeRequestStatus | '' = '';
  filterChangeType: SubscriptionChangeType | '' = '';
  searchQuery = '';

  showApproveDialog = false;
  showRejectDialog = false;
  selectedRequest: AdminPlanUpgradeRequest | null = null;
  adminNote = '';
  adminOverrideReason = '';

  readonly statusFilterOptions: (PlanUpgradeRequestStatus | '')[] = [
    '',
    'PendingPayment',
    'PaymentSubmitted',
    'UnderReview',
    'Activated',
    'Rejected',
    'Cancelled',
    'Expired'
  ];

  readonly changeTypeFilterOptions: (SubscriptionChangeType | '')[] = ['', 'Upgrade', 'Renewal'];

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
        this.error = getApiErrorMessage(err, this.translate.instant('admin.subscriptionChangesError'));
        this.loading = false;
      }
    });
  }

  get filteredRequests(): AdminPlanUpgradeRequest[] {
    const query = this.searchQuery.trim().toLowerCase();

    return this.requests.filter(request => {
      if (this.filterStatus && request.status !== this.filterStatus) {
        return false;
      }

      if (this.filterChangeType && request.changeType !== this.filterChangeType) {
        return false;
      }

      if (!query) {
        return true;
      }

      return (
        request.guildName.toLowerCase().includes(query) ||
        request.requestedByUsername.toLowerCase().includes(query) ||
        request.requestedByDiscordUserId.toLowerCase().includes(query) ||
        request.guildId.toLowerCase().includes(query)
      );
    });
  }

  openApproveDialog(request: AdminPlanUpgradeRequest): void {
    if (!this.isReviewable(request) || this.processingId) {
      return;
    }

    this.selectedRequest = request;
    this.adminNote = '';
    this.adminOverrideReason = '';
    this.showApproveDialog = true;
  }

  closeApproveDialog(): void {
    if (this.processingId) {
      return;
    }

    this.showApproveDialog = false;
    this.selectedRequest = null;
  }

  confirmApprove(): void {
    if (!this.selectedRequest || this.processingId) {
      return;
    }

    if (this.requiresOverrideReason(this.selectedRequest) && !this.adminOverrideReason.trim()) {
      this.toast.error(this.translate.instant('admin.overrideReasonRequired'));
      return;
    }

    this.processingId = this.selectedRequest.id;

    this.adminService
      .approveUpgradeRequest(this.selectedRequest.id, {
        adminNote: this.adminNote.trim() || null,
        adminOverrideReason: this.adminOverrideReason.trim() || null
      })
      .subscribe({
        next: updated => {
          this.requests = this.requests.map(item => (item.id === updated.id ? updated : item));
          this.processingId = null;
          this.showApproveDialog = false;
          this.selectedRequest = null;
          this.toast.success(
            this.translate.instant('admin.changeApproved', {
              guild: updated.guildName,
              plan: updated.requestedPlanName
            })
          );
        },
        error: err => {
          this.processingId = null;
          this.toast.error(getApiErrorMessage(err, this.translate.instant('admin.changeReviewError')));
        }
      });
  }

  openRejectDialog(request: AdminPlanUpgradeRequest): void {
    if (!this.isReviewable(request) || this.processingId) {
      return;
    }

    this.selectedRequest = request;
    this.adminNote = '';
    this.showRejectDialog = true;
  }

  closeRejectDialog(): void {
    if (this.processingId) {
      return;
    }

    this.showRejectDialog = false;
    this.selectedRequest = null;
  }

  confirmReject(): void {
    if (!this.selectedRequest || this.processingId) {
      return;
    }

    if (!this.adminNote.trim()) {
      this.toast.error(this.translate.instant('admin.rejectReasonRequired'));
      return;
    }

    this.processingId = this.selectedRequest.id;

    this.adminService
      .rejectUpgradeRequest(this.selectedRequest.id, {
        adminNote: this.adminNote.trim()
      })
      .subscribe({
        next: updated => {
          this.requests = this.requests.map(item => (item.id === updated.id ? updated : item));
          this.processingId = null;
          this.showRejectDialog = false;
          this.selectedRequest = null;
          this.toast.success(
            this.translate.instant('admin.changeRejected', {
              guild: updated.guildName,
              plan: updated.requestedPlanName
            })
          );
        },
        error: err => {
          this.processingId = null;
          this.toast.error(getApiErrorMessage(err, this.translate.instant('admin.changeReviewError')));
        }
      });
  }

  isProcessing(request: AdminPlanUpgradeRequest): boolean {
    return this.processingId === request.id;
  }

  isReviewable(request: AdminPlanUpgradeRequest): boolean {
    return isReviewableUpgradeRequestStatus(request.status);
  }

  requiresOverrideReason(request: AdminPlanUpgradeRequest): boolean {
    return request.status === 'PendingPayment' || !request.paymentReference?.trim();
  }

  hasPaymentReference(request: AdminPlanUpgradeRequest): boolean {
    return !!request.paymentReference?.trim();
  }

  statusLabel(status: AdminPlanUpgradeRequest['status']): string {
    return this.translate.instant(`subscription.requestStatus.${status.toLowerCase()}`);
  }

  changeTypeLabel(changeType: SubscriptionChangeType): string {
    return this.translate.instant(`subscription.changeType.${changeType.toLowerCase()}`);
  }

  durationLabel(months: number): string {
    return this.translate.instant('subscription.durationMonths', { count: months });
  }

  statusFilterLabel(status: PlanUpgradeRequestStatus | ''): string {
    if (!status) {
      return this.translate.instant('admin.filterAllStatuses');
    }

    return this.statusLabel(status);
  }

  changeTypeFilterLabel(changeType: SubscriptionChangeType | ''): string {
    if (!changeType) {
      return this.translate.instant('admin.filterAllChangeTypes');
    }

    return this.changeTypeLabel(changeType);
  }
}
