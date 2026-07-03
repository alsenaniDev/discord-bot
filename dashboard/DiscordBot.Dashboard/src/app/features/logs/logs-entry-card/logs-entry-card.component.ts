import { Component, EventEmitter, Input, Output } from '@angular/core';
import { LogEntry } from '../../../core/models/log.models';
import { StatusBadgeTone } from '../../../shared/ui/status-badge/status-badge.component';

@Component({
  selector: 'app-logs-entry-card',
  templateUrl: './logs-entry-card.component.html',
  styleUrls: ['./logs-entry-card.component.css']
})
export class LogsEntryCardComponent {
  @Input() log!: LogEntry;
  @Input() selected = false;
  @Input() severity: StatusBadgeTone = 'neutral';
  @Input() severityLabel = '';
  @Input() iconName = 'logs';
  @Input() actorLabel = '';
  @Input() targetLabel = '';

  @Output() select = new EventEmitter<void>();
}
