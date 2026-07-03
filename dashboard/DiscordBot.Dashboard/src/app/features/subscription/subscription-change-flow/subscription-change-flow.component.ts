import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { PlanUpgradeRequest } from '../../../core/models/upgrade-request.models';

@Component({
  selector: 'app-subscription-change-flow',
  templateUrl: './subscription-change-flow.component.html',
  styleUrls: ['./subscription-change-flow.component.css']
})
export class SubscriptionChangeFlowComponent {
  @Input() currentChange!: PlanUpgradeRequest;
  @Input() stepperIndex = 0;
  @Input() paymentReference = '';
  @Input() submittingPayment = false;
  @Input() cancelling = false;

  @Output() paymentReferenceChange = new EventEmitter<string>();
  @Output() submitPayment = new EventEmitter<void>();
  @Output() cancelRequest = new EventEmitter<void>();

  readonly stepperSteps = ['request', 'payment', 'proof', 'review', 'active'] as const;

  constructor(private translate: TranslateService) {}

  isStepComplete(step: number): boolean {
    return this.stepperIndex > step;
  }

  isStepCurrent(step: number): boolean {
    return this.stepperIndex === step;
  }

  get stateTitleKey(): string {
    const status = this.currentChange.status;
    if (status === 'PendingPayment') {
      return 'subscription.states.pendingPayment.title';
    }
    if (status === 'PaymentSubmitted') {
      return 'subscription.states.paymentSubmitted.title';
    }
    if (status === 'UnderReview') {
      return 'subscription.states.underReview.title';
    }
    if (status === 'Approved') {
      return 'subscription.states.approved.title';
    }
    return 'subscription.states.requested.title';
  }

  get stateBodyKey(): string {
    const status = this.currentChange.status;
    if (status === 'PendingPayment') {
      return 'subscription.states.pendingPayment.body';
    }
    if (status === 'PaymentSubmitted') {
      return 'subscription.states.paymentSubmitted.body';
    }
    if (status === 'UnderReview') {
      return 'subscription.states.underReview.body';
    }
    if (status === 'Approved') {
      return 'subscription.states.approved.body';
    }
    return 'subscription.states.requested.body';
  }

  get changeSummaryKey(): string {
    return 'subscription.states.changeSummary';
  }

  get stateParams(): Record<string, string | number> {
    return {
      plan: this.currentChange.requestedPlanName,
      type: this.translate.instant(`subscription.changeType.${this.currentChange.changeType.toLowerCase()}`),
      duration: this.translate.instant('subscription.durationMonths', { count: this.currentChange.durationMonths }),
      total: this.currentChange.estimatedTotalPrice,
      reference: this.currentChange.paymentReference ?? ''
    };
  }

  stateIcon(): string {
    switch (this.currentChange.status) {
      case 'PendingPayment':
        return 'subscription';
      case 'PaymentSubmitted':
      case 'UnderReview':
        return 'clock';
      case 'Approved':
        return 'check-circle';
      default:
        return 'clock';
    }
  }

  stateTone(): string {
    switch (this.currentChange.status) {
      case 'PendingPayment':
        return 'warning';
      case 'PaymentSubmitted':
      case 'UnderReview':
        return 'info';
      case 'Approved':
        return 'success';
      default:
        return 'neutral';
    }
  }

  onPaymentReferenceInput(value: string): void {
    this.paymentReferenceChange.emit(value);
  }
}
