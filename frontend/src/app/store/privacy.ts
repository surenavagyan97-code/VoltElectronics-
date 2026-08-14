import { Component, OnInit, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { I18nService } from '../core/i18n.service';

/** Renders the admin-editable "privacy" content page; the body's own line breaks are kept. */
@Component({
  selector: 'app-privacy',
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
export class PrivacyPage implements OnInit {
  i18n = inject(I18nService);
  private api = inject(ApiClient);

  body = signal('');
  loading = signal(true);

  async ngOnInit(): Promise<void> {
    try {
      this.body.set((await firstValueFrom(this.api.getContent('privacy'))).body);
    } catch {
      this.body.set('');
    } finally {
      this.loading.set(false);
    }
  }
}
