import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="empty-state ds-empty card scale-in">
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
}
