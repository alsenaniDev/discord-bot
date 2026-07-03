import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TicketTranscriptComponent } from './ticket-transcript.component';

const routes: Routes = [
  { path: '', component: TicketTranscriptComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TicketTranscriptRoutingModule {}
