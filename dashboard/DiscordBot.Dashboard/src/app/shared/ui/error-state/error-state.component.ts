import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-error-state',
  template: `
    <app-empty-state
      icon="⚠️"
      [title]="title"
      [description]="description"
      [nested]="nested"
    >
      <button
        *ngIf="retryLabel"
        type="button"
        class="btn btn-primary btn-sm"
        (click)="retry.emit()"
      >
        {{ retryLabel }}
      </button>
      <ng-content></ng-content>
    </app-empty-state>
  `
})
export class ErrorStateComponent {
  @Input() title = '';
  @Input() description = '';
  @Input() retryLabel = '';
  @Input() nested = false;

  @Output() retry = new EventEmitter<void>();
}
