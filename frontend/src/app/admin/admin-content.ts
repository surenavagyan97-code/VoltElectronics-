import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { extractError } from '../core/cart-store';
import { I18nService, LANG_LABELS, Lang } from '../core/i18n.service';

const PAGE_KEYS = ['privacy', 'about', 'faq', 'jobs', 'service'] as const;
const PAGE_LABEL_KEYS: Record<(typeof PAGE_KEYS)[number], string> = {
  privacy: 'footer.privacy',
  about: 'footer.aboutUs',
  faq: 'footer.faq',
  jobs: 'footer.jobs',
  service: 'footer.service',
};

/**
 * Editor for the storefront's admin-editable pages, one body per page + language. English is
 * the fallback shoppers see until a translation is written.
 */
@Component({
  selector: 'app-admin-content',
  imports: [FormsModule],
  template: `
    <div style="max-width: 900px;">
      <h2 style="margin-bottom: 6px;">{{ i18n.t('admin.content.title') }}</h2>
      <p class="text-muted" style="font-size: 13px; margin-bottom: 18px;">{{ i18n.t('admin.content.hint') }}</p>

      <div class="row" style="gap: 10px; margin-bottom: 14px; flex-wrap: wrap;">
        <select class="input" style="width: 240px;" [ngModel]="pageKey()" (ngModelChange)="selectPage($event)">
          @for (key of pageKeys; track key) {
            <option [value]="key">{{ i18n.t(pageLabelKeys[key]) }}</option>
          }
        </select>
        <div class="seg" style="width: fit-content;">
          @for (l of langs; track l) {
            <label class="seg-opt">
              <input type="radio" name="contentLang" [value]="l" [ngModel]="lang()" (ngModelChange)="selectLang($event)" />
              {{ langLabels[l] }}
            </label>
          }
        </div>
      </div>

      @if (lang() !== 'en' && !body.trim()) {
        <p class="text-muted" style="font-size: 13px; margin-bottom: 10px;">{{ i18n.t('admin.content.fallbackNote') }}</p>
      }
      @if (error(); as err) { <div class="error-text" style="margin-bottom: 10px;">{{ err }}</div> }

      <textarea class="input" style="width: 100%; min-height: 440px; font-size: 13px; line-height: 1.6; resize: vertical;"
                [(ngModel)]="body" [disabled]="loading()"></textarea>

      <div class="row" style="gap: 12px; margin-top: 16px;">
        <button class="btn btn-primary" [disabled]="busy() || loading()" (click)="save()">
          {{ busy() ? i18n.t('admin.content.saving') : i18n.t('admin.content.save') }}
        </button>
        @if (saved()) { <span class="text-muted" style="font-size: 13px;">{{ i18n.t('admin.content.saved') }}</span> }
      </div>
    </div>
  `,
})
export class AdminContentPage implements OnInit {
  i18n = inject(I18nService);
  private api = inject(ApiClient);

  readonly pageKeys = PAGE_KEYS;
  readonly pageLabelKeys = PAGE_LABEL_KEYS;
  readonly langs: Lang[] = ['en', 'hy', 'ru'];
  readonly langLabels = LANG_LABELS;

  pageKey = signal<(typeof PAGE_KEYS)[number]>('privacy');
  lang = signal<Lang>('en');
  body = '';
  loading = signal(true);
  busy = signal(false);
  saved = signal(false);
  error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async selectPage(key: (typeof PAGE_KEYS)[number]): Promise<void> {
    this.pageKey.set(key);
    await this.load();
  }

  async selectLang(lang: Lang): Promise<void> {
    this.lang.set(lang);
    await this.load();
  }

  async save(): Promise<void> {
    this.busy.set(true);
    this.saved.set(false);
    this.error.set(null);
    try {
      await firstValueFrom(this.api.adminUpdateContent(this.pageKey(), this.lang(), this.body));
      this.saved.set(true);
    } catch (e) {
      this.error.set(extractError(e));
    } finally {
      this.busy.set(false);
    }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.saved.set(false);
    this.error.set(null);
    try {
      // No fallback: the editor must show "not written yet" as empty, not the English text.
      this.body = (await firstValueFrom(this.api.getContent(this.pageKey(), this.lang(), false))).body;
    } catch {
      this.body = '';
    } finally {
      this.loading.set(false);
    }
  }
}
