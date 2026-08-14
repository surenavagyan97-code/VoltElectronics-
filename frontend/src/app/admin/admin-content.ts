import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { extractError } from '../core/cart-store';
import { I18nService } from '../core/i18n.service';

/** Editor for the storefront's admin-editable pages — currently the privacy policy. */
@Component({
  selector: 'app-admin-content',
  imports: [FormsModule],
  template: `
    <div style="max-width: 860px;">
      <h2 style="margin-bottom: 6px;">{{ i18n.t('admin.content.title') }}</h2>
      <p class="text-muted" style="font-size: 13px; margin-bottom: 18px;">{{ i18n.t('admin.content.hint') }}</p>

      @if (error(); as err) { <div class="error-text" style="margin-bottom: 10px;">{{ err }}</div> }

      <textarea class="input" style="width: 100%; min-height: 480px; font-size: 13px; line-height: 1.6; resize: vertical;"
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

  body = '';
  loading = signal(true);
  busy = signal(false);
  saved = signal(false);
  error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      this.body = (await firstValueFrom(this.api.getContent('privacy'))).body;
    } catch {
      this.body = '';
    } finally {
      this.loading.set(false);
    }
  }

  async save(): Promise<void> {
    this.busy.set(true);
    this.saved.set(false);
    this.error.set(null);
    try {
      await firstValueFrom(this.api.adminUpdateContent('privacy', this.body));
      this.saved.set(true);
    } catch (e) {
      this.error.set(extractError(e));
    } finally {
      this.busy.set(false);
    }
  }
}
