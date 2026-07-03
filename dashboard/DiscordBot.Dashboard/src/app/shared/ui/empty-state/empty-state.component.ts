import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="empty-state ds-empty" [class.card]="!nested" [class.scale-in]="!nested" [class.empty-state-nested]="nested">
      <div class="empty-icon" *ngIf="icon">{{ icon }}</div>
      <h2>{{ title }}</h2>
      <p class="muted" *ngIf="description">{{ description }}</p>
      <ng-content></ng-content>
    </div>
  `
})
export class EmptyStateComponent {
  @Input() icon = '';
  @Input() title = '';
  @Input() description = '';
  /** When true, renders a compact inset empty state for use inside cards. */
  @Input() nested = false;
}
