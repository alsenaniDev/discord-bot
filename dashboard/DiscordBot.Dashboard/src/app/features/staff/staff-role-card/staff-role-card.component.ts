import { Component, EventEmitter, Input, Output } from '@angular/core';
import { StatusBadgeTone } from '../../../shared/ui/status-badge/status-badge.component';

@Component({
  selector: 'app-staff-role-card',
  templateUrl: './staff-role-card.component.html',
  styleUrls: ['./staff-role-card.component.css']
})
export class StaffRoleCardComponent {
  @Input() roleName = '';
  @Input() discordLabel = '';
  @Input() discordColor = '#99aab5';
  @Input() permissionSummary = '';
  @Input() permissionCount = 0;
  @Input() statusLabel = '';
  @Input() statusTone: StatusBadgeTone = 'success';
  @Input() updatedLabel = '';
  @Input() editing = false;
  @Input() selected = false;

  @Output() select = new EventEmitter<void>();
}
