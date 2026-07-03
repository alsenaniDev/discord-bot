import { Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { StatusBadgeTone } from '../../../shared/ui/status-badge/status-badge.component';

@Component({
  selector: 'app-moderation-detail-panel',
  templateUrl: './moderation-detail-panel.component.html'
})
export class ModerationDetailPanelComponent {
  @Input() actionLabel = '';
  @Input() badgeTone: StatusBadgeTone = 'warning';
  @Input() targetLabel = '';
  @Input() moderatorLabel = '';
  @Input() reason = '';
  @Input() evidence = '';
  @Input() durationLabel = '';
  @Input() createdAt = '';
  @Input() entryId = '';
  @Input() targetUserId = '';
  @Input() moderatorUserId = '';
  @Input() channelId = '';
  @Input() messageCountLabel = '';
  @Input() open = false;
  @Input() inline = false;

  @Output() closePanel = new EventEmitter<void>();

  @ViewChild('detailTitle') detailTitle?: ElementRef<HTMLElement>;

  advancedExpanded = false;

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closePanel.emit();
    }
  }

  onAdvancedToggle(event: Event): void {
    this.advancedExpanded = (event.target as HTMLDetailsElement).open;
  }

  focusTitle(): void {
    requestAnimationFrame(() => {
      this.detailTitle?.nativeElement.focus({ preventScroll: true });
    });
  }
}
