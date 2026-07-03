import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnDestroy,
  Output,
  ViewChild
} from '@angular/core';
import {
  ContextDrawerModel,
  ContextDrawerModuleRow,
  ContextDrawerModuleStatus
} from '../../../../core/models/mission-control.models';

const FOCUSABLE_SELECTOR = 'button:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])';

@Component({
  selector: 'app-context-drawer',
  templateUrl: './context-drawer.component.html',
  styleUrls: ['./context-drawer.component.css']
})
export class ContextDrawerComponent implements OnDestroy {
  @Input() model: ContextDrawerModel | null = null;
  @Output() navigate = new EventEmitter<string>();
  @Output() openChange = new EventEmitter<boolean>();

  @ViewChild('panel') panelRef?: ElementRef<HTMLElement>;
  @ViewChild('toggleButton') toggleRef?: ElementRef<HTMLButtonElement>;

  isOpen = false;
  private previouslyFocused: HTMLElement | null = null;
  private focusTrapHandler = (event: KeyboardEvent) => this.handleFocusTrap(event);

  ngOnDestroy(): void {
    this.detachFocusTrap();
    document.body.classList.remove('context-drawer-scroll-lock');
  }

  toggleDrawer(): void {
    if (this.isOpen) {
      this.close();
      return;
    }

    this.open();
  }

  open(): void {
    this.isOpen = true;
    this.openChange.emit(true);
    this.previouslyFocused = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    this.syncScrollLock();
    this.attachFocusTrap();

    requestAnimationFrame(() => {
      const first = this.getFocusableElements()[0];
      first?.focus();
    });
  }

  close(): void {
    if (!this.isOpen) {
      return;
    }

    this.isOpen = false;
    this.openChange.emit(false);
    this.detachFocusTrap();
    this.syncScrollLock();
    this.toggleRef?.nativeElement.focus();
    this.previouslyFocused = null;
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.close();
    }
  }

  onModuleRowClick(route: string): void {
    this.navigate.emit(route);
  }

  onSuggestionClick(route: string): void {
    this.navigate.emit(route);
  }

  onNavigate(route: string): void {
    this.navigate.emit(route);
  }

  statusIcon(status: ContextDrawerModuleStatus): string {
    if (status === 'enabled') {
      return 'check';
    }

    if (status === 'warning') {
      return 'alert-circle';
    }

    return 'x';
  }

  statusLabelKey(status: ContextDrawerModuleStatus): string {
    return `overview.v2.drawer.modules.status.${status}`;
  }

  moduleRowClass(row: ContextDrawerModuleRow): string {
    return row.status === 'disabled' ? 'context-drawer-module-row is-muted' : 'context-drawer-module-row';
  }

  formatExpiry(isoDate: string | null | undefined): string {
    if (!isoDate) {
      return '';
    }

    const date = new Date(isoDate);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return new Intl.DateTimeFormat(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(date);
  }

  @HostListener('document:keydown', ['$event'])
  onDocumentKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape' && this.isOpen) {
      event.preventDefault();
      this.close();
    }
  }

  private syncScrollLock(): void {
    const isOverlay = typeof window !== 'undefined' && window.matchMedia('(max-width: 1023px)').matches;
    document.body.classList.toggle('context-drawer-scroll-lock', this.isOpen && isOverlay);
  }

  private attachFocusTrap(): void {
    document.addEventListener('keydown', this.focusTrapHandler, true);
  }

  private detachFocusTrap(): void {
    document.removeEventListener('keydown', this.focusTrapHandler, true);
  }

  private handleFocusTrap(event: KeyboardEvent): void {
    if (!this.isOpen || event.key !== 'Tab' || !this.panelRef) {
      return;
    }

    const focusable = this.getFocusableElements();
    if (focusable.length === 0) {
      return;
    }

    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    const active = document.activeElement;

    if (event.shiftKey && active === first) {
      event.preventDefault();
      last.focus();
      return;
    }

    if (!event.shiftKey && active === last) {
      event.preventDefault();
      first.focus();
    }
  }

  private getFocusableElements(): HTMLElement[] {
    if (!this.panelRef) {
      return [];
    }

    return Array.from(this.panelRef.nativeElement.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR));
  }
}
