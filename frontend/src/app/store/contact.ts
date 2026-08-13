import { Component, inject } from '@angular/core';
import { I18nService } from '../core/i18n.service';

@Component({
  selector: 'app-contact',
  template: `
    <div style="max-width: 760px; margin: 0 auto; padding: 48px 32px;">
      <h2 style="margin-bottom: 8px;">{{ i18n.t('contact.title') }}</h2>
      <p class="text-muted" style="margin-bottom: 28px;">{{ i18n.t('contact.subtitle') }}</p>

      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 14px;">
        <div class="card">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--color-accent)" stroke-width="1.6"><rect x="2" y="4" width="20" height="16" rx="2"></rect><path d="m22 7-10 6L2 7"></path></svg>
          <div class="card-title" style="font-size: 14px;">{{ i18n.t('contact.email') }}</div>
          <a class="card-meta" href="mailto:info@smartbuy.am" style="color: var(--color-accent);">info&#64;smartbuy.am</a>
        </div>
        <div class="card">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--color-accent)" stroke-width="1.6"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.12.9.34 1.79.66 2.64a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.44-1.23a2 2 0 0 1 2.11-.45c.85.32 1.74.54 2.64.66A2 2 0 0 1 22 16.92z"></path></svg>
          <div class="card-title" style="font-size: 14px;">{{ i18n.t('contact.phone') }}</div>
          <a class="card-meta" href="tel:+37410203040" style="color: var(--color-accent);">+374 10 20 30 40</a>
        </div>
        <div class="card">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--color-accent)" stroke-width="1.6"><path d="M20 10c0 6-8 12-8 12S4 16 4 10a8 8 0 0 1 16 0z"></path><circle cx="12" cy="10" r="3"></circle></svg>
          <div class="card-title" style="font-size: 14px;">{{ i18n.t('contact.address') }}</div>
          <div class="card-meta">{{ i18n.t('contact.addressValue') }}</div>
        </div>
        <div class="card">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--color-accent)" stroke-width="1.6"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
          <div class="card-title" style="font-size: 14px;">{{ i18n.t('contact.hours') }}</div>
          <div class="card-meta">{{ i18n.t('contact.hoursValue') }}</div>
        </div>
      </div>
    </div>
  `,
})
export class ContactPage {
  i18n = inject(I18nService);
}
