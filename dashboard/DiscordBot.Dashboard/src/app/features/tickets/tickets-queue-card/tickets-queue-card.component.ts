import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TicketSummaryReadModel } from '../../../core/models/ticket.models';

@Component({
  selector: 'app-tickets-queue-card',
  templateUrl: './tickets-queue-card.component.html',
  styleUrls: ['./tickets-queue-card.component.css']
})
export class TicketsQueueCardComponent {
  @Input() ticket!: TicketSummaryReadModel;
  @Input() selected = false;
  @Input() open = false;
  @Input() statusLabelKey = '';
  @Input() ownerLabel = '';
  @Input() preview = '';

  @Output() select = new EventEmitter<void>();
}
