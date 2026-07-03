import { Component, Input } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { ToastService } from '../../../core/services/toast.service';
import { MANUAL_BILLING_CONFIG } from '../config/manual-billing.config';

@Component({
  selector: 'app-subscription-payment-instructions',
  templateUrl: './subscription-payment-instructions.component.html',
  styleUrls: ['./subscription-payment-instructions.component.css']
})
export class SubscriptionPaymentInstructionsComponent {
  @Input() amount = 0;
  @Input() paymentReferenceHint = '';

  readonly config = MANUAL_BILLING_CONFIG;

  constructor(
    private toast: ToastService,
    private translate: TranslateService
  ) {}

  async copyValue(value: string, labelKey: string): Promise<void> {
    if (!value?.trim()) {
      return;
    }

    try {
      await navigator.clipboard.writeText(value.replace(/\s/g, ''));
      this.toast.success(this.translate.instant('subscription.paymentPanel.copied', {
        field: this.translate.instant(labelKey)
      }));
    } catch {
      this.toast.error(this.translate.instant('subscription.paymentPanel.copyFailed'));
    }
  }

  copyAmount(): void {
    void this.copyValue(String(this.amount), 'subscription.paymentPanel.amount');
  }

  formattedAmount(): string {
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: 'USD'
    }).format(this.amount);
  }
}
