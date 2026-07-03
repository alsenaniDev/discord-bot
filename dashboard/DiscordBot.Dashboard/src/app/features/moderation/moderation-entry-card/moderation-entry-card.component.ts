import { Component, EventEmitter, Input, Output } from '@angular/core';
import { StatusBadgeTone } from '../../../shared/ui/status-badge/status-badge.component';

@Component({
  selector: 'app-moderation-entry-card',
  templateUrl: './moderation-entry-card.component.html'
})
export class ModerationEntryCardComponent {
  @Input() actionLabel = '';
  @Input() targetLabel = '';
  @Input() moderatorLabel = '';
  @Input() reason = '';
  @Input() createdAt = '';
  @Input() selected = false;
  @Input() badgeTone: StatusBadgeTone = 'warning';
  @Input() iconTone: 'success' | 'warning' | 'danger' | 'info' = 'warning';

  @Output() select = new EventEmitter<void>();
}
