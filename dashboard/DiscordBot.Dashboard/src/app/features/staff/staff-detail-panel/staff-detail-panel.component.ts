import { Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { StatusBadgeTone } from '../../../shared/ui/status-badge/status-badge.component';

export interface StaffPermissionGroupView {
  id: string;
  labelKey: string;
  items: Array<{ labelKey: string; granted: boolean }>;
}

@Component({
  selector: 'app-staff-detail-panel',
  templateUrl: './staff-detail-panel.component.html',
  styleUrls: ['./staff-detail-panel.component.css']
})
export class StaffDetailPanelComponent {
  @Input() roleName = '';
  @Input() discordLabel = '';
  @Input() discordColor = '#99aab5';
  @Input() permissionCount = 0;
  @Input() statusLabel = '';
  @Input() statusTone: StatusBadgeTone = 'success';
  @Input() updatedLabel = '';
  @Input() permissionGroups: StaffPermissionGroupView[] = [];
  @Input() roleId = '';
  @Input() discordRoleId = '';
  @Input() createdAt = '';
  @Input() open = false;
  @Input() inline = false;
  @Input() editing = false;

  @Output() closePanel = new EventEmitter<void>();
  @Output() editPermissions = new EventEmitter<void>();
  @Output() deleteMapping = new EventEmitter<void>();
  @Output() scrollToEditor = new EventEmitter<void>();

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
