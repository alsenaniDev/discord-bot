import { Component, Input } from '@angular/core';

export type StatusBadgeTone =
  | 'success'
  | 'warning'
  | 'danger'
  | 'info'
  | 'neutral'
  | 'brand'
  | 'premium'
  | 'locked'
  | 'enabled'
  | 'disabled'
  | 'open'
  | 'closed'
  | 'muted';

@Component({
  selector: 'app-status-badge',
  template: `
    <span class="badge" [attr.data-status]="tone" [class.badge-open]="tone === 'open'">
      {{ label }}
    </span>
  `
})
export class StatusBadgeComponent {
  @Input() label = '';
  @Input() tone: StatusBadgeTone = 'neutral';
}
