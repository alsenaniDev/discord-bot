import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type AppLanguage = 'en' | 'ar';

const STORAGE_KEY = 'dashboard_lang';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  readonly supportedLanguages: AppLanguage[] = ['en', 'ar'];

  constructor(private translate: TranslateService) {}

  init(): void {
    this.translate.addLangs(this.supportedLanguages);
    this.translate.setDefaultLang('en');

    const saved = localStorage.getItem(STORAGE_KEY) as AppLanguage | null;
    const initial = saved && this.supportedLanguages.includes(saved)
      ? saved
      : this.detectBrowserLanguage();

    this.applyLanguage(initial, false);
  }

  get currentLanguage(): AppLanguage {
    return (this.translate.currentLang as AppLanguage) || 'en';
  }

  setLanguage(lang: AppLanguage): void {
    this.applyLanguage(lang, true);
  }

  toggleLanguage(): void {
    this.setLanguage(this.currentLanguage === 'en' ? 'ar' : 'en');
  }

  isRtl(): boolean {
    return this.currentLanguage === 'ar';
  }

  private detectBrowserLanguage(): AppLanguage {
    const browserLang = (navigator.language || 'en').toLowerCase();
    return browserLang.startsWith('ar') ? 'ar' : 'en';
  }

  private applyLanguage(lang: AppLanguage, persist: boolean): void {
    this.translate.use(lang);
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';

    if (persist) {
      localStorage.setItem(STORAGE_KEY, lang);
    }
  }
}
