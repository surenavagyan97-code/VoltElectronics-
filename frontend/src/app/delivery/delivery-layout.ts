import { Component, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AuthStore } from '../core/auth-store';
import { I18nService } from '../core/i18n.service';
import { LangSelect } from '../core/lang-select';
import { ThemeStore } from '../core/theme-store';

@Component({
  selector: 'app-delivery-layout',
  imports: [RouterOutlet, LangSelect],
  template: `
    <div style="min-height: 100vh; display: flex; flex-direction: column;">
      <header class="row" style="justify-content: space-between; padding: 14px 20px; background: var(--color-surface); border-bottom: 1px solid var(--color-divider); gap: 12px; flex-wrap: wrap;">
        <div class="brand-logo">
          <span class="brand-tile" aria-hidden="true">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor"><path d="M13.5 2 4.5 13.5h5.2L10.5 22l9-11.5h-5.2L13.5 2z"/></svg>
          </span>
          <span class="brand-word" style="font-size: 17px;">Smart<em>Buy</em></span>
          <span class="text-muted" style="font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; margin-top: 3px;">{{ i18n.t('delivery.brand') }}</span>
        </div>
        <div class="row" style="gap: 10px;">
          <span class="text-muted" style="font-size: 13px;">{{ auth.user()?.fullName }}</span>
          <app-lang-select />
          <button class="btn btn-icon btn-ghost" (click)="theme.toggle()" [attr.aria-label]="i18n.t(theme.theme() === 'dark' ? 'theme.lightTheme' : 'theme.darkTheme')">
            @if (theme.theme() === 'dark') {
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="12" cy="12" r="4"></circle><path d="M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1"></path></svg>
            } @else {
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M21 12.5A9 9 0 1 1 11.5 3a7 7 0 0 0 9.5 9.5z"></path></svg>
            }
          </button>
          <button class="btn btn-secondary" (click)="logout()">{{ i18n.t('delivery.signOut') }}</button>
        </div>
      </header>
      <main style="flex: 1; padding: 24px 20px; max-width: 900px; width: 100%; margin: 0 auto;">
        <router-outlet />
      </main>
    </div>
  `,
})
export class DeliveryLayout {
  auth = inject(AuthStore);
  theme = inject(ThemeStore);
  i18n = inject(I18nService);
  private router = inject(Router);

  async logout(): Promise<void> {
    await this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
