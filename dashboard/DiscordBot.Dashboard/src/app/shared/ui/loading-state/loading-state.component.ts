import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loading-state',
  template: `
    <div class="loading-panel ds-loading" role="status" [attr.aria-label]="message">
      <span class="spinner spinner-lg" *ngIf="!skeleton"></span>
      <div class="skeleton-stack" *ngIf="skeleton">
        <div class="skeleton skeleton-title"></div>
        <div class="skeleton skeleton-text"></div>
        <div class="skeleton skeleton-text" style="width: 80%"></div>
        <div class="skeleton skeleton-card"></div>
      </div>
      <p>{{ message }}</p>
    </div>
  `,
  styles: [`
    .skeleton-stack { width: min(420px, 100%); }
    .skeleton-stack .skeleton-text { width: 100%; }
  `]
})
export class LoadingStateComponent {
  @Input() message = '';
  @Input() skeleton = false;
}
