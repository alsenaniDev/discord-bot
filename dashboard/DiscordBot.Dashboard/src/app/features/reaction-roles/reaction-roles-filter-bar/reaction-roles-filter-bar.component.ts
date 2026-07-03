import { Component, EventEmitter, Input, Output } from '@angular/core';

export interface ReactionRolesUiFilters {
  search: string;
  status: 'all' | 'active' | 'inactive';
  channel: string;
}

@Component({
  selector: 'app-reaction-roles-filter-bar',
  templateUrl: './reaction-roles-filter-bar.component.html',
  styleUrls: ['./reaction-roles-filter-bar.component.css']
})
export class ReactionRolesFilterBarComponent {
  @Input() ui: ReactionRolesUiFilters = { search: '', status: 'all', channel: '' };
  @Input() channelOptions: { id: string; label: string }[] = [];
  @Input() disabled = false;
  @Input() resultCount = 0;

  @Output() apply = new EventEmitter<void>();
  @Output() clear = new EventEmitter<void>();

  readonly statusFilters: Array<'all' | 'active' | 'inactive'> = ['all', 'active', 'inactive'];

  statusLabelKey(filter: 'all' | 'active' | 'inactive'): string {
    return `reactionRoles.workspace.status.${filter}`;
  }

  onApply(): void {
    this.apply.emit();
  }

  onClear(): void {
    this.clear.emit();
  }
}
