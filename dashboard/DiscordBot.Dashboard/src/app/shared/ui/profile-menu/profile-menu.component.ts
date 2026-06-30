import { Component, ElementRef, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { UserProfile } from '../../../core/models/auth.models';

@Component({
  selector: 'app-profile-menu',
  template: `
    <div class="ds-dropdown">
      <button
        type="button"
        class="profile-trigger"
        (click)="toggle($event)"
        [attr.aria-label]="'common.profile' | translate"
        [attr.aria-expanded]="open"
      >
        <span class="profile-avatar">{{ initials }}</span>
        <span class="profile-name hide-mobile">{{ displayName }}</span>
        <app-ui-icon name="chevron-down" size="sm"></app-ui-icon>
      </button>
      <div class="ds-dropdown-menu profile-menu" *ngIf="open" role="menu">
        <div class="profile-menu-header">
          <strong>{{ displayName }}</strong>
          <span class="muted small">{{ user?.username }}</span>
        </div>
        <button type="button" class="ds-dropdown-item danger" (click)="onLogout()" role="menuitem">
          <app-ui-icon name="logout" size="sm"></app-ui-icon>
          {{ 'common.logout' | translate }}
        </button>
      </div>
    </div>
  `,
  styles: [`
    .profile-trigger {
      display: inline-flex;
      align-items: center;
      gap: var(--space-2);
      padding: 0.35rem 0.5rem 0.35rem 0.35rem;
      border: 1px solid var(--color-border);
      border-radius: var(--radius-full);
      background: var(--color-bg-panel);
      color: var(--color-text);
      cursor: pointer;
      font: inherit;
      transition: border-color var(--duration-fast) var(--ease-out);
    }
    .profile-trigger:hover { border-color: var(--color-border-strong); }
    .profile-avatar {
      width: 1.75rem;
      height: 1.75rem;
      border-radius: 50%;
      background: linear-gradient(135deg, var(--color-brand), #7289da);
      display: inline-flex;
      align-items: center;
      justify-content: center;
      font-size: var(--text-xs);
      font-weight: 700;
      color: var(--color-text-on-brand);
    }
    .profile-name {
      font-size: var(--text-sm);
      font-weight: 600;
      max-width: 120px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .profile-menu { min-width: 220px; }
    .profile-menu-header {
      padding: 0.5rem 0.75rem 0.75rem;
      border-bottom: 1px solid var(--color-border);
      margin-bottom: var(--space-2);
    }
    @media (max-width: 640px) {
      .hide-mobile { display: none; }
    }
  `]
})
export class ProfileMenuComponent {
  @Input() user: UserProfile | null = null;
  @Output() logoutClick = new EventEmitter<void>();
  open = false;

  constructor(private el: ElementRef) {}

  get displayName(): string {
    return this.user?.globalName || this.user?.username || '';
  }

  get initials(): string {
    const name = this.displayName;
    return name ? name.charAt(0).toUpperCase() : '?';
  }

  toggle(event: Event): void {
    event.stopPropagation();
    this.open = !this.open;
  }

  onLogout(): void {
    this.open = false;
    this.logoutClick.emit();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!this.el.nativeElement.contains(event.target)) {
      this.open = false;
    }
  }
}
