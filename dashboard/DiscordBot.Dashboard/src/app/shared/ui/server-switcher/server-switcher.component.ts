import { Component, ElementRef, HostListener, Input } from '@angular/core';
import { Router } from '@angular/router';
import { GuildSummary } from '../../../core/models/guild.models';
import { GuildContextService } from '../../../core/services/guild-context.service';

@Component({
  selector: 'app-server-switcher',
  template: `
    <div class="ds-dropdown switcher" *ngIf="guilds.length > 0">
      <button
        type="button"
        class="switcher-trigger"
        (click)="toggle($event)"
        [attr.aria-expanded]="open"
      >
        <span class="switcher-avatar" [class.has-image]="selected?.iconUrl">
          <img *ngIf="selected?.iconUrl" [src]="selected!.iconUrl" [alt]="selected!.name" />
          <span *ngIf="!selected?.iconUrl">{{ initial }}</span>
        </span>
        <span class="switcher-label">{{ selected?.name || ('nav.servers' | translate) }}</span>
        <app-ui-icon name="chevron-down" size="sm"></app-ui-icon>
      </button>
      <div class="ds-dropdown-menu switcher-menu" *ngIf="open">
        <a
          class="ds-dropdown-item"
          routerLink="/servers"
          (click)="close()"
        >
          <app-ui-icon name="servers" size="sm"></app-ui-icon>
          {{ 'nav.servers' | translate }}
        </a>
        <div class="switcher-divider"></div>
        <button
          type="button"
          class="ds-dropdown-item"
          *ngFor="let guild of guilds"
          (click)="selectGuild(guild)"
          [class.active]="guild.id === selected?.id"
        >
          <span class="switcher-avatar sm" [class.has-image]="guild.iconUrl">
            <img *ngIf="guild.iconUrl" [src]="guild.iconUrl" [alt]="guild.name" />
            <span *ngIf="!guild.iconUrl">{{ guild.name.charAt(0) }}</span>
          </span>
          {{ guild.name }}
        </button>
      </div>
    </div>
  `,
  styles: [`
    .switcher-trigger {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      width: 100%;
      padding: 0.5rem 0.65rem;
      border: 1px solid var(--color-border);
      border-radius: var(--radius-md);
      background: var(--color-bg-panel);
      color: var(--color-text);
      cursor: pointer;
      font: inherit;
      text-align: start;
      transition: border-color var(--duration-fast) var(--ease-out);
    }
    .switcher-trigger:hover { border-color: var(--color-border-strong); }
    .switcher-label {
      flex: 1;
      font-size: var(--text-sm);
      font-weight: 600;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .switcher-avatar {
      width: 1.5rem;
      height: 1.5rem;
      border-radius: var(--radius-sm);
      background: var(--color-brand-soft);
      color: var(--color-text-brand);
      display: inline-flex;
      align-items: center;
      justify-content: center;
      font-size: var(--text-xs);
      font-weight: 700;
      overflow: hidden;
      flex-shrink: 0;
    }
    .switcher-avatar.sm { width: 1.25rem; height: 1.25rem; }
    .switcher-avatar img { width: 100%; height: 100%; object-fit: cover; }
    .switcher-menu { width: 100%; min-width: 240px; }
    .switcher-divider { height: 1px; background: var(--color-border); margin: var(--space-2) 0; }
    .ds-dropdown-item.active { background: var(--color-brand-soft); color: var(--color-text-brand); }
  `]
})
export class ServerSwitcherComponent {
  @Input() guilds: GuildSummary[] = [];
  @Input() selected: GuildSummary | null = null;
  open = false;

  constructor(
    private router: Router,
    private guildContext: GuildContextService,
    private el: ElementRef
  ) {}

  get initial(): string {
    return this.selected?.name?.charAt(0)?.toUpperCase() || 'S';
  }

  toggle(event: Event): void {
    event.stopPropagation();
    this.open = !this.open;
  }

  close(): void {
    this.open = false;
  }

  selectGuild(guild: GuildSummary): void {
    this.guildContext.selectGuild(guild);
    this.router.navigate(['/guilds', guild.id, 'overview']);
    this.close();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!this.el.nativeElement.contains(event.target)) {
      this.open = false;
    }
  }
}
