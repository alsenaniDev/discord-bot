import { Component, EventEmitter, Input, Output } from '@angular/core';
import {
  PageWorkspaceHeroAction,
  PageWorkspaceHeroBadge,
  PageWorkspaceHeroFooterTone,
  PageWorkspaceHeroIconName,
  PageWorkspaceHeroStat
} from './page-workspace-hero.models';

@Component({
  selector: 'app-page-workspace-hero',
  templateUrl: './page-workspace-hero.component.html',
  styleUrls: ['./page-workspace-hero.component.css']
})
export class PageWorkspaceHeroComponent {
  @Input() ariaLabel = '';
  @Input() icon: PageWorkspaceHeroIconName = 'overview';
  @Input() iconUrl: string | null = null;
  @Input() title = '';
  @Input() description = '';
  @Input() stats: PageWorkspaceHeroStat[] = [];
  @Input() footerMessage = '';
  @Input() footerTone: PageWorkspaceHeroFooterTone = 'success';
  @Input() badge: PageWorkspaceHeroBadge | null = null;
  @Input() primaryAction: PageWorkspaceHeroAction | null = null;
  @Input() dismissible = false;
  @Input() loading = false;

  @Output() primaryActionClick = new EventEmitter<void>();
  @Output() dismissClick = new EventEmitter<void>();

  onPrimaryActionClick(): void {
    if (this.primaryAction?.type === 'submit') {
      return;
    }

    if (this.primaryAction?.disabled || this.primaryAction?.loading || this.primaryAction?.hidden) {
      return;
    }

    this.primaryActionClick.emit();
  }

  onDismissClick(): void {
    this.dismissClick.emit();
  }
}
