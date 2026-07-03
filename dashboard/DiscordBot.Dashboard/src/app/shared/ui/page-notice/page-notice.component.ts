import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-page-notice',
  template: `
    <p
      class="ws-page-notice muted small"
      [class.ws-page-notice--accent]="accent"
      role="note"
    >
      <ng-content></ng-content>
    </p>
  `
})
export class PageNoticeComponent {
  @Input() accent = false;
}
