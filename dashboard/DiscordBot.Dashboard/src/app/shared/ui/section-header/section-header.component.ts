import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-section-header',
  template: `
    <header
      class="ws-section-head"
      [class.ws-section-head--compact]="compact"
    >
      <h2
        [class]="titleClasses"
        [id]="titleId || null"
      >
        {{ title }}
      </h2>
      <p
        class="ws-section-lead muted"
        [class.ws-section-lead--wide]="wideLead"
        *ngIf="lead"
      >
        {{ lead }}
      </p>
      <ng-content></ng-content>
    </header>
  `
})
export class SectionHeaderComponent {
  @Input() title = '';
  @Input() lead = '';
  @Input() titleId = '';
  @Input() compact = false;
  @Input() emphasis = false;
  @Input() wideLead = false;

  get titleClasses(): string {
    return this.emphasis
      ? 'ws-section-title ws-section-title--emphasis'
      : 'ws-section-title';
  }
}
