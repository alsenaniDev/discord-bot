import { Component, EventEmitter, Input, Output } from '@angular/core';

export type TicketsStatusFilter = 'all' | 'Open' | 'Closed';

@Component({
  selector: 'app-tickets-filter-bar',
  templateUrl: './tickets-filter-bar.component.html',
  styleUrls: ['./tickets-filter-bar.component.css']
})
export class TicketsFilterBarComponent {
  @Input() statusFilter: TicketsStatusFilter = 'all';
  @Input() totalCount = 0;
  @Input() page = 1;
  @Input() totalPages = 0;
  @Input() disabled = false;

  @Output() statusChange = new EventEmitter<TicketsStatusFilter>();
  @Output() prevPage = new EventEmitter<void>();
  @Output() nextPage = new EventEmitter<void>();

  readonly filters: TicketsStatusFilter[] = ['all', 'Open', 'Closed'];

  filterLabelKey(filter: TicketsStatusFilter): string {
    switch (filter) {
      case 'Open':
        return 'tickets.workspace.filterOpen';
      case 'Closed':
        return 'tickets.workspace.filterClosed';
      default:
        return 'tickets.workspace.filterAll';
    }
  }

  onFilterClick(filter: TicketsStatusFilter): void {
    if (this.disabled || filter === this.statusFilter) {
      return;
    }

    this.statusChange.emit(filter);
  }

  onPrevPage(): void {
    if (this.disabled || this.page <= 1) {
      return;
    }

    this.prevPage.emit();
  }

  onNextPage(): void {
    if (this.disabled || this.page >= this.totalPages) {
      return;
    }

    this.nextPage.emit();
  }
}
