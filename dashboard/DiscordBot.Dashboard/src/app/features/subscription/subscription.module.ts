import { NgModule } from '@angular/core';
import { SubscriptionComponent } from './subscription.component';
import { SubscriptionChangeFlowComponent } from './subscription-change-flow/subscription-change-flow.component';
import { SubscriptionHistoryComponent } from './subscription-history/subscription-history.component';
import { SubscriptionPaymentInstructionsComponent } from './subscription-payment-instructions/subscription-payment-instructions.component';
import { SubscriptionRoutingModule } from './subscription-routing.module';
import { SharedUiModule } from '../../shared/shared-ui.module';

@NgModule({
  declarations: [
    SubscriptionComponent,
    SubscriptionChangeFlowComponent,
    SubscriptionHistoryComponent,
    SubscriptionPaymentInstructionsComponent
  ],
  imports: [
    SharedUiModule,
    SubscriptionRoutingModule
  ]
})
export class SubscriptionModule {}
