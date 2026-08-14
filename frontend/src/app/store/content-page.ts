import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { I18nService } from '../core/i18n.service';

/**
 * Renders any admin-editable page (privacy, about, faq, jobs, service) by route key, in the
 * shopper's language with server-side fallback to English. Line breaks in the body are kept.
 */
@Component({
  selector: 'app-content-page',
  template: `
    <div style="max-width: 820px; margin: 0 auto; padding: 48px 32px;">
      @if (loading()) {
        <div class="row" style="justify-content: center; padding: 60px;"><div class="spinner"></div></div>
      } @else {
        <div style="white-space: pre-wrap; font-size: 14px; line-height: 1.75;">{{ body() }}</div>
      }
    </div>
  `,
})
export class ContentPagePage {
  i18n = inject(I18nService);
  private api = inject(ApiClient);
  private route = inject(ActivatedRoute);

  body = signal('');
  loading = signal(true);

  constructor() {
    // Subscribed so in-app navigation between footer pages re-renders without a reload.
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      void this.load(params.get('key') ?? '');
    });
  }

  private async load(key: string): Promise<void> {
    this.loading.set(true);
    try {
      this.body.set((await firstValueFrom(this.api.getContent(key))).body);
    } catch {
      this.body.set('');
    } finally {
      this.loading.set(false);
    }
  }
}
