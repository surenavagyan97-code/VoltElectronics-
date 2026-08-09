import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthStore } from '../core/auth-store';
import { CartStore } from '../core/cart-store';
import { ThemeStore } from '../core/theme-store';

@Component({
  selector: 'app-store-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="nav" style="border-bottom: 1px solid var(--color-divider); padding: 14px 32px;">
      <a routerLink="/" class="nav-brand" style="color: inherit;">Volt Electronics</a>
      <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Home</a>
      <a routerLink="/shop" routerLinkActive="active">Shop</a>
      @if (auth.isLoggedIn() && !auth.isAdmin()) {
        <a routerLink="/account/orders" routerLinkActive="active">Orders</a>
      }
      @if (auth.isAdmin()) {
        <a routerLink="/admin">Admin</a>
      }
      <div style="flex: 1;"></div>
      <div class="row" style="gap: 18px;">
        <button
          type="button" class="btn btn-icon btn-ghost" style="color: inherit;"
          (click)="theme.toggle()"
          [attr.aria-label]="theme.theme() === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'"
        >
          @if (theme.theme() === 'dark') {
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="12" cy="12" r="4"></circle><path d="M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1"></path></svg>
          } @else {
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M21 12.5A9 9 0 1 1 11.5 3a7 7 0 0 0 9.5 9.5z"></path></svg>
          }
        </button>
        <a routerLink="/cart" style="position: relative; color: inherit; display: block;" aria-label="Cart">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="9" cy="21" r="1"></circle><circle cx="20" cy="21" r="1"></circle><path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path></svg>
          @if (cart.count() > 0) {
            <div style="position: absolute; top: -6px; right: -8px; background: var(--color-accent); color: var(--color-bg); font-size: 10px; font-weight: 600; border-radius: 999px; min-width: 16px; height: 16px; display: flex; align-items: center; justify-content: center; padding: 0 3px;">{{ cart.count() }}</div>
          }
        </a>
        @if (auth.isLoggedIn()) {
          <div class="row" style="gap: 10px;">
            <span class="text-muted" style="font-size: 13px;">{{ auth.user()?.fullName }}</span>
            <button class="btn btn-ghost" style="font-size: 13px;" (click)="logout()">Sign out</button>
          </div>
        } @else {
          <a routerLink="/login" class="row" style="gap: 6px; color: inherit;" aria-label="Sign in">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="12" cy="8" r="4"></circle><path d="M4 21c0-4 4-6 8-6s8 2 8 6"></path></svg>
            <span style="font-size: 13px;">Sign in</span>
          </a>
        }
      </div>
    </div>

    <router-outlet />

    <div style="border-top: 1px solid var(--color-divider); padding: 28px 32px; opacity: 0.6; font-size: 13px;">
      © 2026 Volt Electronics. Procurement-friendly pricing on request.
    </div>
  `,
})
export class StoreLayout {
  auth = inject(AuthStore);
  cart = inject(CartStore);
  theme = inject(ThemeStore);
  private router = inject(Router);

  async logout(): Promise<void> {
    await this.auth.logout();
    await this.cart.reloadAsGuest();
    void this.router.navigateByUrl('/');
  }
}
