import { Component, Input } from '@angular/core';

type IconName =
  | 'home'
  | 'servers'
  | 'overview'
  | 'settings'
  | 'tickets'
  | 'shield'
  | 'modules'
  | 'subscription'
  | 'logs'
  | 'roles'
  | 'admin'
  | 'users'
  | 'guilds'
  | 'chevron-right'
  | 'chevron-down'
  | 'menu'
  | 'bell'
  | 'globe'
  | 'logout'
  | 'external'
  | 'bot'
  | 'check'
  | 'x'
  | 'refresh'
  | 'check-circle'
  | 'alert-circle'
  | 'clock'
  | 'cloud-off'
  | 'lock';

@Component({
  selector: 'app-ui-icon',
  template: `
    <svg
      class="ui-icon"
      [class.ui-icon-sm]="size === 'sm'"
      [class.ui-icon-lg]="size === 'lg'"
      [attr.width]="sizePx"
      [attr.height]="sizePx"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      <ng-container [ngSwitch]="name">
        <g *ngSwitchCase="'home'"><path d="M3 9.5 12 3l9 6.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1V9.5z"/></g>
        <g *ngSwitchCase="'servers'"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></g>
        <g *ngSwitchCase="'overview'"><path d="M4 19V5"/><path d="M4 19h16"/><path d="M8 17V9"/><path d="M12 17V7"/><path d="M16 17v-5"/></g>
        <g *ngSwitchCase="'settings'"><circle cx="12" cy="12" r="3"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></g>
        <g *ngSwitchCase="'tickets'"><path d="M15 5H9a2 2 0 0 0-2 2v10l3-2 3 2 3-2 3 2V7a2 2 0 0 0-2-2z"/></g>
        <g *ngSwitchCase="'shield'"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></g>
        <g *ngSwitchCase="'modules'"><path d="M12 2 2 7l10 5 10-5-10-5z"/><path d="m2 17 10 5 10-5"/><path d="m2 12 10 5 10-5"/></g>
        <g *ngSwitchCase="'subscription'"><rect x="2" y="5" width="20" height="14" rx="2"/><path d="M2 10h20"/></g>
        <g *ngSwitchCase="'logs'"><path d="M14 2H6a2 2 0 0 0-2 2v16l4-2 4 2 4-2 4 2V8z"/></g>
        <g *ngSwitchCase="'roles'"><circle cx="12" cy="8" r="4"/><path d="M4 20c0-4 3.6-6 8-6s8 2 8 6"/></g>
        <g *ngSwitchCase="'admin'"><path d="M12 2v4"/><path d="m4.93 4.93 2.83 2.83"/><path d="M2 12h4"/><path d="m4.93 19.07 2.83-2.83"/><path d="M12 18v4"/><path d="m19.07 19.07-2.83-2.83"/><path d="M18 12h4"/><path d="m19.07 4.93-2.83 2.83"/></g>
        <g *ngSwitchCase="'users'"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></g>
        <g *ngSwitchCase="'guilds'"><path d="M3 21h18"/><path d="M6 21V7l6-3 6 3v14"/><path d="M9 21v-4h6v4"/></g>
        <g *ngSwitchCase="'chevron-right'"><path d="m9 18 6-6-6-6"/></g>
        <g *ngSwitchCase="'chevron-down'"><path d="m6 9 6 6 6-6"/></g>
        <g *ngSwitchCase="'menu'"><path d="M4 6h16M4 12h16M4 18h16"/></g>
        <g *ngSwitchCase="'bell'"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/></g>
        <g *ngSwitchCase="'globe'"><circle cx="12" cy="12" r="10"/><path d="M2 12h20"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/></g>
        <g *ngSwitchCase="'logout'"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></g>
        <g *ngSwitchCase="'external'"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/><polyline points="15 3 21 3 21 9"/><line x1="10" y1="14" x2="21" y2="3"/></g>
        <g *ngSwitchCase="'bot'"><rect x="4" y="8" width="16" height="12" rx="2"/><path d="M9 8V6a3 3 0 0 1 6 0v2"/><circle cx="9" cy="14" r="1"/><circle cx="15" cy="14" r="1"/></g>
        <g *ngSwitchCase="'check'"><polyline points="20 6 9 17 4 12"/></g>
        <g *ngSwitchCase="'x'"><path d="M18 6 6 18M6 6l12 12"/></g>
        <g *ngSwitchCase="'refresh'"><path d="M21 12a9 9 0 1 1-2.64-6.36"/><path d="M21 3v6h-6"/></g>
        <g *ngSwitchCase="'check-circle'"><circle cx="12" cy="12" r="10"/><polyline points="9 12 11 14 15 10"/></g>
        <g *ngSwitchCase="'alert-circle'"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></g>
        <g *ngSwitchCase="'clock'"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></g>
        <g *ngSwitchCase="'cloud-off'"><path d="m2 2 20 20"/><path d="M8.5 8.5a5 5 0 0 1 7 7"/><path d="M2 15a5 5 0 0 1 8.4-3.6"/><path d="M12 22a5 5 0 0 0 4.9-4"/></g>
        <g *ngSwitchCase="'lock'"><rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/></g>
      </ng-container>
    </svg>
  `,
  styles: [`
    :host { display: inline-flex; align-items: center; justify-content: center; }
    .ui-icon { display: block; }
    .ui-icon-sm { width: 16px; height: 16px; }
    .ui-icon-lg { width: 22px; height: 22px; }
  `]
})
export class UiIconComponent {
  @Input() name: IconName = 'home';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';

  get sizePx(): number {
    return this.size === 'sm' ? 16 : this.size === 'lg' ? 22 : 18;
  }
}
