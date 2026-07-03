import { Component, EventEmitter, Input, Output } from '@angular/core';
import { StaffPermissionGroupId } from '../staff-permission-groups';

export type StaffRoleFilter = 'all' | 'owner' | 'moderator' | 'support';
export type StaffStatusFilter = 'all' | 'active' | 'inactive';

@Component({
  selector: 'app-staff-filter-bar',
  templateUrl: './staff-filter-bar.component.html',
  styleUrls: ['./staff-filter-bar.component.css']
})
export class StaffFilterBarComponent {
  @Input() search = '';
  @Input() roleFilter: StaffRoleFilter = 'all';
  @Input() statusFilter: StaffStatusFilter = 'all';
  @Input() permissionFilter: StaffPermissionGroupId | 'all' = 'all';
  @Input() resultCount = 0;
  @Input() disabled = false;

  @Output() searchChange = new EventEmitter<string>();
  @Output() roleFilterChange = new EventEmitter<StaffRoleFilter>();
  @Output() statusFilterChange = new EventEmitter<StaffStatusFilter>();
  @Output() permissionFilterChange = new EventEmitter<StaffPermissionGroupId | 'all'>();
  @Output() clear = new EventEmitter<void>();

  readonly roleFilters: StaffRoleFilter[] = ['all', 'owner', 'moderator', 'support'];
  readonly statusFilters: StaffStatusFilter[] = ['all', 'active', 'inactive'];
  readonly permissionFilters: Array<StaffPermissionGroupId | 'all'> = [
    'all',
    'moderation',
    'tickets',
    'logs',
    'modules',
    'settings'
  ];

  onClear(): void {
    this.clear.emit();
  }
}
