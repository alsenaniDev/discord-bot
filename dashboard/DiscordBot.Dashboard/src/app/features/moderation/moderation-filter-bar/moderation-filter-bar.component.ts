import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ModerationFilters } from '../../../core/models/moderation.models';

@Component({
  selector: 'app-moderation-filter-bar',
  templateUrl: './moderation-filter-bar.component.html',
  styleUrls: ['./moderation-filter-bar.component.css']
})
export class ModerationFilterBarComponent {
  @Input() guildId = '';
  @Input() filters: ModerationFilters = { targetUserId: '', type: '', from: '', to: '' };
  @Input() moderatorUserId = '';
  @Input() search = '';
  @Input() disabled = false;
  @Input() resultCount = 0;

  @Output() apply = new EventEmitter<void>();
  @Output() clear = new EventEmitter<void>();

  onApply(): void {
    this.apply.emit();
  }

  onClear(): void {
    this.clear.emit();
  }
}
