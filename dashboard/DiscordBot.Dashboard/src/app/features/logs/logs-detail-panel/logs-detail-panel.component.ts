import { Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { LogEntry } from '../../../core/models/log.models';
import { StatusBadgeTone } from '../../../shared/ui/status-badge/status-badge.component';

@Component({
  selector: 'app-logs-detail-panel',
  templateUrl: './logs-detail-panel.component.html',
  styleUrls: ['./logs-detail-panel.component.css']
})
export class LogsDetailPanelComponent {
  @Input() log: LogEntry | null = null;
  @Input() open = false;
  @Input() inline = false;
  @Input() severity: StatusBadgeTone = 'neutral';
  @Input() severityLabel = '';
  @Input() actorLabel = '';
  @Input() targetLabel = '';
  @Input() channelLabel = '';
  @Input() metadataPreview = '';
  @Input() canClear = false;

  @Output() closePanel = new EventEmitter<void>();
  @Output() clearAll = new EventEmitter<void>();

  @ViewChild('detailTitle') detailTitle?: ElementRef<HTMLElement>;

  advancedExpanded = false;

  onAdvancedToggle(event: Event): void {
    this.advancedExpanded = (event.target as HTMLDetailsElement).open;
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closePanel.emit();
    }
  }

  focusTitle(): void {
    requestAnimationFrame(() => {
      this.detailTitle?.nativeElement.focus({ preventScroll: true });
    });
  }
}
