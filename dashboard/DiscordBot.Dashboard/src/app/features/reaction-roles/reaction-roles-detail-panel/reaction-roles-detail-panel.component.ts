import { Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';

@Component({
  selector: 'app-reaction-roles-detail-panel',
  templateUrl: './reaction-roles-detail-panel.component.html'
})
export class ReactionRolesDetailPanelComponent {
  @Input() title = '';
  @Input() description = '';
  @Input() buttonLabel = '';
  @Input() channelLabel = '';
  @Input() roleLabel = '';
  @Input() messageStatusLabel = '';
  @Input() messageLinked = false;
  @Input() active = false;
  @Input() messageId = '';
  @Input() channelId = '';
  @Input() buttonCustomId = '';
  @Input() createdAt = '';
  @Input() open = false;
  @Input() inline = false;
  @Input() canOpen = false;
  @Input() canCopy = false;
  @Input() deactivating = false;

  @Output() closePanel = new EventEmitter<void>();
  @Output() edit = new EventEmitter<void>();
  @Output() openDiscord = new EventEmitter<void>();
  @Output() copyLink = new EventEmitter<void>();
  @Output() disable = new EventEmitter<void>();

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
