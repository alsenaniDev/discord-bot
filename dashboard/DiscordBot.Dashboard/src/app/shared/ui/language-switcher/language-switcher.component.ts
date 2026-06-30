import { Component, ElementRef, HostListener } from '@angular/core';
import { LanguageService, AppLanguage } from '../../../core/services/language.service';

@Component({
  selector: 'app-language-switcher',
  template: `
    <div class="ds-dropdown">
      <button
        type="button"
        class="icon-btn lang-btn"
        (click)="toggle($event)"
        [attr.aria-label]="'common.switchLanguage' | translate"
        [attr.aria-expanded]="open"
      >
        <app-ui-icon name="globe" size="sm"></app-ui-icon>
        <span class="lang-code">{{ currentLanguage | uppercase }}</span>
      </button>
      <div class="ds-dropdown-menu" *ngIf="open" role="menu">
        <button
          type="button"
          class="ds-dropdown-item"
          *ngFor="let lang of languages"
          (click)="select(lang)"
          [class.active]="lang === currentLanguage"
          role="menuitem"
        >
          {{ label(lang) | translate }}
        </button>
      </div>
    </div>
  `,
  styles: [`
    .lang-btn { width: auto; padding: 0 0.65rem; gap: 0.35rem; }
    .lang-code { font-size: var(--text-xs); font-weight: 700; color: var(--color-text-secondary); }
    .ds-dropdown-item.active { background: var(--color-brand-soft); color: var(--color-text-brand); }
  `]
})
export class LanguageSwitcherComponent {
  open = false;
  languages: AppLanguage[] = ['en', 'ar'];

  constructor(public language: LanguageService, private el: ElementRef) {}

  get currentLanguage(): AppLanguage {
    return this.language.currentLanguage;
  }

  toggle(event: Event): void {
    event.stopPropagation();
    this.open = !this.open;
  }

  select(lang: AppLanguage): void {
    this.language.setLanguage(lang);
    this.open = false;
  }

  label(lang: AppLanguage): string {
    return lang === 'ar' ? 'common.arabic' : 'common.english';
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!this.el.nativeElement.contains(event.target)) {
      this.open = false;
    }
  }
}
