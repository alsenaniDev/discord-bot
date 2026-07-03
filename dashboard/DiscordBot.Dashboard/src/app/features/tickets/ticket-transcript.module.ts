import { NgModule } from '@angular/core';
import { TicketTranscriptComponent } from './ticket-transcript.component';
import { TicketTranscriptRoutingModule } from './ticket-transcript-routing.module';
import { SharedUiModule } from '../../shared/shared-ui.module';

@NgModule({
  declarations: [TicketTranscriptComponent],
  imports: [
    SharedUiModule,
    TicketTranscriptRoutingModule
  ]
})
export class TicketTranscriptModule {}
