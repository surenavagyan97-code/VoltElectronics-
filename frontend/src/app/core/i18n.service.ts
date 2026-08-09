import { Injectable, signal } from '@angular/core';
import { en } from './i18n/en';
import { hy } from './i18n/hy';
import { ru } from './i18n/ru';

const STORAGE_KEY = 'volt.lang';

export type Lang = 'en' | 'hy' | 'ru';

const DICTS: Record<Lang, Record<string, string>> = { en, hy, ru };
export const LANG_LABELS: Record<Lang, string> = { en: 'English', hy: 'Հայերեն', ru: 'Русский' };

@Injectable({ providedIn: 'root' })
export class I18nService {
  readonly lang = signal<Lang>(readStored());

  constructor() {
    document.documentElement.lang = this.lang();
  }

  setLang(lang: Lang): void {
    this.lang.set(lang);
    localStorage.setItem(STORAGE_KEY, lang);
    document.documentElement.lang = lang;
  }

  /** Looks up `key` in the current language, falling back to English, then the raw key. */
  t(key: string, params?: Record<string, string | number>): string {
    let str = DICTS[this.lang()][key] ?? DICTS.en[key] ?? key;
    if (params) {
      for (const [k, v] of Object.entries(params)) str = str.replaceAll(`{${k}}`, String(v));
    }
    return str;
  }
}

function readStored(): Lang {
  const v = localStorage.getItem(STORAGE_KEY);
  return v === 'hy' || v === 'ru' ? v : 'en';
}
