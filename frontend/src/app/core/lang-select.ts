import { Component, ElementRef, HostListener, inject, input, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { I18nService, LANG_LABELS, Lang } from './i18n.service';

/**
 * Language picker with real SVG flags. A native <select> can't render images in its options,
 * and flag emoji don't render on Windows (Chrome falls back to "GB"/"AM"/"RU" letter codes),
 * so this is a small custom dropdown instead.
 */
@Component({
  selector: 'app-lang-select',
  imports: [NgTemplateOutlet],
  styles: `
    :host { position: relative; display: inline-block; }
    .lang-btn {
      display: flex; align-items: center; gap: 6px; cursor: pointer;
      height: 32px; padding: 2px 8px; font-size: 12px; width: 100%;
    }
    .flag { width: 18px; height: 12px; border-radius: 2px; flex: none; box-shadow: 0 0 0 1px var(--color-divider); }
    .caret { margin-left: auto; opacity: 0.6; flex: none; }
    .lang-menu {
      position: absolute; top: calc(100% + 4px); left: 0; min-width: 100%; z-index: 50;
      display: flex; flex-direction: column; gap: 2px; padding: 4px;
      background: var(--color-surface); border: 1px solid var(--color-divider);
      border-radius: 8px; box-shadow: 0 6px 24px rgba(0, 0, 0, 0.14);
    }
    .lang-menu.up { top: auto; bottom: calc(100% + 4px); }
    .lang-opt {
      display: flex; align-items: center; gap: 8px; padding: 6px 8px; white-space: nowrap;
      border: none; background: none; cursor: pointer; border-radius: 6px;
      font: inherit; font-size: 12px; color: var(--color-text); text-align: left;
    }
    .lang-opt:hover { background: color-mix(in srgb, var(--color-accent) 10%, transparent); }
    .lang-opt.selected { color: var(--color-accent); font-weight: 600; }
  `,
  template: `
    <ng-template #flag let-l>
      @switch (l) {
        @case ('en') {
          <svg class="flag" viewBox="0 0 60 40" aria-hidden="true">
            <rect width="60" height="40" fill="#012169"/>
            <path d="M0 0 60 40M60 0 0 40" stroke="#fff" stroke-width="8"/>
            <path d="M0 0 60 40M60 0 0 40" stroke="#C8102E" stroke-width="4"/>
            <path d="M30 0v40M0 20h60" stroke="#fff" stroke-width="13"/>
            <path d="M30 0v40M0 20h60" stroke="#C8102E" stroke-width="7"/>
          </svg>
        }
        @case ('hy') {
          <svg class="flag" viewBox="0 0 60 40" aria-hidden="true">
            <rect width="60" height="14" fill="#D90012"/>
            <rect y="13" width="60" height="14" fill="#0033A0"/>
            <rect y="26" width="60" height="14" fill="#F2A800"/>
          </svg>
        }
        @case ('ru') {
          <svg class="flag" viewBox="0 0 60 40" aria-hidden="true">
            <rect width="60" height="14" fill="#fff"/>
            <rect y="13" width="60" height="14" fill="#0039A6"/>
            <rect y="26" width="60" height="14" fill="#D52B1E"/>
          </svg>
        }
      }
    </ng-template>

    <button type="button" class="lang-btn input" (click)="open.set(!open())"
            [attr.aria-label]="i18n.t('lang.label')" [attr.aria-expanded]="open()" aria-haspopup="listbox">
      <ng-container *ngTemplateOutlet="flag; context: { $implicit: i18n.lang() }" />
      {{ labels[i18n.lang()] }}
      <svg class="caret" width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4"><polyline points="6 9 12 15 18 9"></polyline></svg>
    </button>

    @if (open()) {
      <div class="lang-menu" [class.up]="direction() === 'up'" role="listbox">
        @for (l of langs; track l) {
          <button type="button" class="lang-opt" role="option"
                  [class.selected]="l === i18n.lang()" [attr.aria-selected]="l === i18n.lang()"
                  (click)="choose(l)">
            <ng-container *ngTemplateOutlet="flag; context: { $implicit: l }" />
            {{ labels[l] }}
          </button>
        }
      </div>
    }
  `,
})
export class LangSelect {
  i18n = inject(I18nService);
  private host = inject(ElementRef<HTMLElement>);

  /** Which way the menu unfolds — 'up' for pickers sitting near the bottom of the viewport. */
  direction = input<'down' | 'up'>('down');

  readonly langs: Lang[] = ['en', 'hy', 'ru'];
  readonly labels = LANG_LABELS;
  open = signal(false);

  choose(lang: Lang): void {
    this.i18n.setLang(lang);
    this.open.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.open() && !this.host.nativeElement.contains(event.target as Node)) this.open.set(false);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.open.set(false);
  }
}
