import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ThemeStore } from '../core/theme-store';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  styles: `
    .admin-nav-link {
      display: flex; align-items: center; gap: 10px; padding: 9px 12px;
      border-radius: var(--radius-md); font-size: 14px; text-decoration: none;
      color: var(--color-text);
    }
    .admin-nav-link.active {
      color: var(--color-accent);
      background: color-mix(in srgb, var(--color-accent) 12%, transparent);
    }
  `,
  template: `
    <div style="display: flex; min-height: 100vh;">
      <div class="col" style="width: 220px; flex: none; background: var(--color-surface); padding: 20px 14px; gap: 4px; border-right: 1px solid var(--color-divider);">
        <div class="nav-brand" style="padding: 0 10px 18px;">Volt Admin</div>
        <a routerLink="/admin/products" routerLinkActive="active" class="admin-nav-link">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M21 8l-9-5-9 5 9 5 9-5z"></path><path d="M3 8v8l9 5 9-5V8"></path><path d="M12 13v8"></path></svg>
          Products
        </a>
        <a routerLink="/admin/categories" routerLinkActive="active" class="admin-nav-link">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><rect x="3" y="3" width="7" height="7"></rect><rect x="14" y="3" width="7" height="7"></rect><rect x="3" y="14" width="7" height="7"></rect><rect x="14" y="14" width="7" height="7"></rect></svg>
          Categories
        </a>
        <a routerLink="/admin/orders" routerLinkActive="active" class="admin-nav-link">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="9" cy="21" r="1"></circle><circle cx="20" cy="21" r="1"></circle><path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path></svg>
          Orders
        </a>
        <a routerLink="/admin/analytics" routerLinkActive="active" class="admin-nav-link">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><line x1="4" y1="20" x2="4" y2="10"></line><line x1="12" y1="20" x2="12" y2="4"></line><line x1="20" y1="20" x2="20" y2="14"></line></svg>
          Analytics
        </a>
        <div style="flex: 1;"></div>
        <button
          type="button" class="admin-nav-link text-muted"
          style="background: none; border: none; cursor: pointer; font: inherit; text-align: left; width: 100%;"
          (click)="theme.toggle()"
        >
          @if (theme.theme() === 'dark') {
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="12" cy="12" r="4"></circle><path d="M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1"></path></svg>
            Light theme
          } @else {
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M21 12.5A9 9 0 1 1 11.5 3a7 7 0 0 0 9.5 9.5z"></path></svg>
            Dark theme
          }
        </button>
        <a routerLink="/" class="admin-nav-link text-muted">← Back to store</a>
      </div>
      <div style="flex: 1; padding: 28px 36px; min-width: 0;">
        <router-outlet />
      </div>
    </div>
  `,
})
export class AdminLayout {
  theme = inject(ThemeStore);
}
