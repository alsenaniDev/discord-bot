import { Component, EventEmitter, Input, Output } from '@angular/core';
import { LogFilters, LOG_EVENT_TYPE_OPTIONS } from '../../../core/models/log.models';

@Component({
  selector: 'app-logs-filter-bar',
  templateUrl: './logs-filter-bar.component.html',
  styleUrls: ['./logs-filter-bar.component.css']
})
export class LogsFilterBarComponent {
  @Input() guildId = '';
  @Input() filters: LogFilters = { type: '', from: '', to: '', search: '', userId: '' };
  @Input() disabled = false;
  @Input() resultCount = 0;

  @Output() apply = new EventEmitter<void>();
  @Output() clear = new EventEmitter<void>();

  typeOptions = LOG_EVENT_TYPE_OPTIONS;

  onApply(): void {
    this.apply.emit();
  }

  onClear(): void {
    this.clear.emit();
  }
}
