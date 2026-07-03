import { Component, Input } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { PlanUpgradeRequest, SubscriptionChangeType } from '../../../core/models/upgrade-request.models';

@Component({
  selector: 'app-subscription-history',
  templateUrl: './subscription-history.component.html',
  styleUrls: ['./subscription-history.component.css']
})
export class SubscriptionHistoryComponent {
  @Input() requests: PlanUpgradeRequest[] = [];

  constructor(private translate: TranslateService) {}

  statusLabel(status: PlanUpgradeRequest['status']): string {
    return this.translate.instant(`subscription.requestStatus.${status.toLowerCase()}`);
  }

  changeTypeLabel(changeType: SubscriptionChangeType): string {
    return this.translate.instant(`subscription.changeType.${changeType.toLowerCase()}`);
  }

  statusTone(status: PlanUpgradeRequest['status']): string {
    switch (status) {
      case 'Activated':
      case 'Approved':
        return 'success';
      case 'Rejected':
        return 'danger';
      case 'PendingPayment':
      case 'PaymentSubmitted':
      case 'UnderReview':
        return 'info';
      case 'Expired':
      case 'Cancelled':
        return 'neutral';
      default:
        return 'neutral';
    }
  }
}
