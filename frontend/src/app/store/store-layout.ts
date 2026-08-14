import { Component, ElementRef, HostListener, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink, RouterOutlet } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { Category } from '../core/api.types';
import { AuthStore } from '../core/auth-store';
import { CartStore } from '../core/cart-store';
import { CompareStore } from '../core/compare-store';
import { Currency, CurrencyStore } from '../core/currency-store';
import { FavoritesStore } from '../core/favorites-store';
import { I18nService } from '../core/i18n.service';
import { LangSelect } from '../core/lang-select';
import { ThemeStore } from '../core/theme-store';

const RECENT_SEARCHES_KEY = 'sb_recent_searches';

function readRecentSearches(): string[] {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(RECENT_SEARCHES_KEY) ?? '[]');
    return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === 'string') : [];
  } catch {
    return [];
  }
}

@Component({
  selector: 'app-store-layout',
  imports: [RouterOutlet, RouterLink, FormsModule, LangSelect],
  styles: `
    /* — pinned header — */
    .site-header {
      position: sticky; top: 0; z-index: 100;
      background: var(--color-bg);
    }

    /* — main header — */
    .mainbar { display: flex; align-items: center; gap: 20px; padding: 14px 32px; flex-wrap: wrap; }
    .brand-block { color: inherit; text-decoration: none; flex: none; }
    .brand-mark { font-weight: 800; font-size: 22px; letter-spacing: 6px; line-height: 1; }
    .brand-mark em {
      font-style: normal;
      background: linear-gradient(95deg, var(--color-accent), var(--color-accent-2));
      -webkit-background-clip: text; background-clip: text; color: transparent;
    }
    .brand-domain { font-size: 10px; letter-spacing: 3px; opacity: 0.55; margin-top: 4px; }
    .searchbox { flex: 1; display: flex; min-width: 240px; max-width: 620px; position: relative; }
    .searchbox .input { flex: 1; border-top-right-radius: 0; border-bottom-right-radius: 0; }
    .searchbox .btn { border-top-left-radius: 0; border-bottom-left-radius: 0; }
    .search-panel {
      position: absolute; top: calc(100% + 6px); left: 0; right: 0; z-index: 60;
      background: var(--color-surface); border: 1px solid var(--color-divider);
      border-radius: 12px; padding: 14px 16px; box-shadow: 0 12px 32px rgba(0, 0, 0, 0.22);
    }
    .search-item {
      display: flex; align-items: center; gap: 9px; width: 100%; text-align: left;
      background: none; border: none; cursor: pointer; padding: 7px 6px;
      color: var(--color-text); font: inherit; font-size: 13px; border-radius: 6px;
    }
    .search-item:hover { background: color-mix(in srgb, var(--color-accent) 10%, transparent); }
    .search-item svg { opacity: 0.55; flex: none; }
    .search-link {
      display: flex; align-items: center; gap: 5px; background: none; border: none;
      cursor: pointer; color: var(--color-accent); font: inherit; font-size: 13px; padding: 0;
    }
    .header-action {
      display: flex; align-items: center; gap: 6px; color: inherit;
      text-decoration: none; font-size: 13px; background: none; border: none;
      cursor: pointer; font: inherit;
    }
    .header-action:hover { color: var(--color-accent); }

    /* — user menu — */
    .user-menu-wrap { position: relative; }
    .user-menu {
      position: absolute; top: calc(100% + 8px); right: 0; min-width: 160px; z-index: 50;
      display: flex; flex-direction: column; gap: 2px; padding: 4px;
      background: var(--color-surface); border: 1px solid var(--color-divider);
      border-radius: 8px; box-shadow: 0 6px 24px rgba(0, 0, 0, 0.14);
    }
    .user-menu-opt {
      display: flex; align-items: center; gap: 8px; padding: 8px 10px; white-space: nowrap;
      border: none; background: none; cursor: pointer; border-radius: 6px; width: 100%;
      font: inherit; font-size: 13px; color: var(--color-text); text-align: left; text-decoration: none;
    }
    .user-menu-opt:hover { background: color-mix(in srgb, var(--color-accent) 10%, transparent); color: var(--color-accent); }

    /* — category chips — */
    .chips {
      display: flex; align-items: center; gap: 10px; flex-wrap: wrap;
      padding: 6px 32px 16px; border-bottom: 1px solid var(--color-divider);
    }
    .chip {
      border: 1px solid color-mix(in srgb, var(--color-accent) 55%, transparent);
      color: var(--color-accent); background: transparent;
      border-radius: 8px; padding: 6px 14px; font-size: 13px; cursor: pointer; font-family: inherit;
    }
    .chip:hover { background: color-mix(in srgb, var(--color-accent) 12%, transparent); }
    .chip.chip-active { background: var(--color-accent); color: var(--color-bg); }

    /* — footer — */
    .footer { border-top: 1px solid var(--color-divider); padding: 40px 32px 0; }
    .footer-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(210px, 1fr)); gap: 32px; }
    .footer h6 { font-size: 11px; letter-spacing: 0.1em; text-transform: uppercase; margin-bottom: 16px; }
    .footer-links { display: flex; flex-direction: column; gap: 10px; font-size: 14px; }
    .footer-links a { color: inherit; text-decoration: none; opacity: 0.8; }
    .footer-links a:hover { color: var(--color-accent); opacity: 1; }
    .footer-bottom {
      border-top: 1px solid var(--color-divider); margin-top: 36px;
      padding: 22px 0; opacity: 0.6; font-size: 13px;
    }
    .pay-pills { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 16px; }
    .pay-pill {
      display: inline-flex; align-items: center; justify-content: center;
      background: #fff; border-radius: 999px; padding: 8px 18px; min-width: 72px; height: 38px;
      box-shadow: inset 0 0 0 1px rgba(0, 0, 0, 0.10);
    }
    .socials { display: flex; gap: 12px; align-items: center; }
    .socials svg { opacity: 0.7; }
    .socials svg:hover { opacity: 1; color: var(--color-accent); }
  `,
  template: `
    <div style="display: flex; flex-direction: column; min-height: 100vh;">

    <div class="site-header">
    <div class="mainbar">
      <a routerLink="/" class="brand-block">
        <div class="brand-mark">SMART<em>BUY</em></div>
        <div class="brand-domain">smartbuy.am</div>
      </a>

      <div class="searchbox" #searchWrap>
        <input class="input" [placeholder]="i18n.t('header.searchPlaceholder')"
               [(ngModel)]="searchTerm" [ngModelOptions]="{ standalone: true }"
               (focus)="searchOpen.set(true)" (keyup.enter)="submitSearch()" />
        <button class="btn btn-primary" (click)="submitSearch()">{{ i18n.t('header.search') }}</button>

        @if (searchOpen()) {
          <div class="search-panel">
            <div class="row" style="justify-content: space-between; margin-bottom: 6px;">
              <strong style="font-size: 14px;">{{ i18n.t('search.recent') }}</strong>
              @if (recent().length) {
                <button class="search-link" (click)="clearHistory()">
                  {{ i18n.t('search.clear') }}
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M3 6h18"></path><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"></path></svg>
                </button>
              }
            </div>
            @if (!recent().length) {
              <div class="text-muted" style="font-size: 13px; padding: 2px 0 8px;">{{ i18n.t('search.noHistory') }}</div>
            } @else {
              <div class="col" style="gap: 2px; margin-bottom: 8px;">
                @for (term of recent(); track term) {
                  <button class="search-item" (click)="runSearch(term)">
                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
                    {{ term }}
                  </button>
                }
              </div>
            }
            <div class="row" style="justify-content: space-between; margin: 6px 0;">
              <strong style="font-size: 14px;">{{ i18n.t('search.recommended') }}</strong>
              <button class="search-link" (click)="refreshRecommendations()">
                {{ i18n.t('search.refresh') }}
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><polyline points="23 4 23 10 17 10"></polyline><path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"></path></svg>
              </button>
            </div>
            <div class="col" style="gap: 2px;">
              @for (name of recommended(); track name) {
                <button class="search-item" (click)="runSearch(name)">
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
                  {{ name }}
                </button>
              }
            </div>
          </div>
        }
      </div>

      <div class="row" style="gap: 18px; flex: none; align-items: center;">
        <a href="tel:+37410203040" class="header-action">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.12.9.34 1.79.66 2.64a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.44-1.23a2 2 0 0 1 2.11-.45c.85.32 1.74.54 2.64.66A2 2 0 0 1 22 16.92z"></path></svg>
          +374 10 20 30 40
        </a>
        <app-lang-select />
        <select class="input" style="height: 32px; padding: 2px 8px; font-size: 12px; width: auto;"
                [ngModel]="currency.currency()" [ngModelOptions]="{ standalone: true }" (ngModelChange)="setCurrency($event)">
          @for (c of currency.supported(); track c) { <option [value]="c">{{ c }}</option> }
        </select>
        <button type="button" class="btn btn-icon header-action" (click)="theme.toggle()"
                [attr.aria-label]="theme.theme() === 'dark' ? i18n.t('theme.toggleToLight') : i18n.t('theme.toggleToDark')">
          @if (theme.theme() === 'dark') {
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="12" cy="12" r="4"></circle><path d="M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1"></path></svg>
          } @else {
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M21 12.5A9 9 0 1 1 11.5 3a7 7 0 0 0 9.5 9.5z"></path></svg>
          }
        </button>
        <a routerLink="/cart" class="header-action" style="position: relative;" [attr.aria-label]="i18n.t('nav.cart')">
          <svg width="19" height="19" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="9" cy="21" r="1"></circle><circle cx="20" cy="21" r="1"></circle><path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path></svg>
          @if (cart.count() > 0) {
            <div style="position: absolute; top: -7px; right: -9px; background: var(--color-accent); color: var(--color-bg); font-size: 10px; font-weight: 600; border-radius: 999px; min-width: 16px; height: 16px; display: flex; align-items: center; justify-content: center; padding: 0 3px;">{{ cart.count() }}</div>
          }
        </a>
        <a routerLink="/favorites" class="header-action" style="position: relative;" [attr.aria-label]="i18n.t('fav.title')">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path></svg>
          @if (favorites.count() > 0) {
            <div style="position: absolute; top: -7px; right: -9px; background: var(--color-accent); color: var(--color-bg); font-size: 10px; font-weight: 600; border-radius: 999px; min-width: 16px; height: 16px; display: flex; align-items: center; justify-content: center; padding: 0 3px;">{{ favorites.count() }}</div>
          }
        </a>
        <a routerLink="/compare" class="header-action" style="position: relative;" [attr.aria-label]="i18n.t('compare.title')">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><line x1="4" y1="20" x2="4" y2="10"></line><line x1="12" y1="20" x2="12" y2="4"></line><line x1="20" y1="20" x2="20" y2="14"></line></svg>
          @if (compare.count() > 0) {
            <div style="position: absolute; top: -7px; right: -9px; background: var(--color-accent); color: var(--color-bg); font-size: 10px; font-weight: 600; border-radius: 999px; min-width: 16px; height: 16px; display: flex; align-items: center; justify-content: center; padding: 0 3px;">{{ compare.count() }}</div>
          }
        </a>
        @if (auth.isCourier()) {
          <a class="header-action" routerLink="/delivery" [attr.aria-label]="i18n.t('nav.delivery')">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><rect x="1" y="3" width="15" height="13" rx="1"></rect><path d="M16 8h4l3 4v4h-7V8z"></path><circle cx="5.5" cy="18.5" r="2"></circle><circle cx="18.5" cy="18.5" r="2"></circle></svg>
          </a>
        }
        @if (auth.isLoggedIn()) {
          <div class="user-menu-wrap" #userMenuWrap>
            <button type="button" class="header-action" (click)="userMenuOpen.set(!userMenuOpen())"
                    [attr.aria-expanded]="userMenuOpen()" aria-haspopup="menu">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="12" cy="8" r="4"></circle><path d="M4 21c0-4 4-6 8-6s8 2 8 6"></path></svg>
              {{ auth.user()?.fullName }}
              <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" style="opacity: 0.6;"><polyline points="6 9 12 15 18 9"></polyline></svg>
            </button>
            @if (userMenuOpen()) {
              <div class="user-menu" role="menu">
                <a routerLink="/account/orders" class="user-menu-opt" role="menuitem" (click)="userMenuOpen.set(false)">{{ i18n.t('nav.profile') }}</a>
                <button type="button" class="user-menu-opt" role="menuitem" (click)="logout()">{{ i18n.t('nav.signOut') }}</button>
              </div>
            }
          </div>
        } @else {
          <a routerLink="/login" class="header-action">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="12" cy="8" r="4"></circle><path d="M4 21c0-4 4-6 8-6s8 2 8 6"></path></svg>
            {{ i18n.t('header.account') }}
          </a>
        }
      </div>
    </div>

    <div class="chips">
      <a class="btn btn-secondary" routerLink="/shop" style="flex: none;">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="3" y1="6" x2="21" y2="6"></line><line x1="3" y1="12" x2="21" y2="12"></line><line x1="3" y1="18" x2="21" y2="18"></line></svg>
        {{ i18n.t('header.allCategory') }}
      </a>
      <span style="opacity: 0.3;">|</span>
      @for (cat of categories(); track cat.id) {
        <button class="chip" [class.chip-active]="selectedCategoryIds().includes(cat.id)" (click)="toggleCategory(cat.id)">{{ cat.name }}</button>
      }
    </div>
    </div>

    <div style="flex: 1;">
      <router-outlet />
    </div>

    <div class="footer">
      <div class="footer-grid">
        <div>
          <h6>{{ i18n.t('footer.toTheBuyer') }}</h6>
          <div class="footer-links">
            <a routerLink="/shop">{{ i18n.t('footer.promotion') }}</a>
            <a routerLink="/contact">{{ i18n.t('footer.delivery') }}</a>
            <a routerLink="/contact">{{ i18n.t('footer.payment') }}</a>
            <a routerLink="/account/orders">{{ i18n.t('footer.order') }}</a>
          </div>
        </div>
        <div>
          <h6>{{ i18n.t('footer.information') }}</h6>
          <div class="footer-links">
            <a routerLink="/pages/about">{{ i18n.t('footer.aboutUs') }}</a>
            <a routerLink="/pages/faq">{{ i18n.t('footer.faq') }}</a>
            <a routerLink="/pages/jobs">{{ i18n.t('footer.jobs') }}</a>
            <a routerLink="/pages/service">{{ i18n.t('footer.service') }}</a>
            <a routerLink="/pages/privacy">{{ i18n.t('footer.privacy') }}</a>
          </div>
        </div>
        <div>
          <h6>{{ i18n.t('footer.contactTitle') }}</h6>
          <div class="footer-links">
            <span style="opacity: 0.8;">{{ i18n.t('contact.addressValue') }}</span>
            <a href="mailto:info@smartbuy.am">info&#64;smartbuy.am</a>
            <a href="tel:+37410203040">+374 10 20 30 40</a>
            <div class="socials" style="margin-top: 4px;">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-label="Facebook"><path d="M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z"></path></svg>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-label="Instagram"><rect x="2" y="2" width="20" height="20" rx="5"></rect><circle cx="12" cy="12" r="4"></circle><line x1="17.5" y1="6.5" x2="17.5" y2="6.5" stroke-linecap="round" stroke-width="2.4"></line></svg>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-label="Chat"><path d="M21 11.5a8.38 8.38 0 0 1-8.5 8.5 8.5 8.5 0 0 1-4-.97L3 21l1.97-5.5a8.5 8.5 0 1 1 16.03-4z"></path></svg>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-label="Email"><rect x="2" y="4" width="20" height="16" rx="2"></rect><path d="m22 7-10 6L2 7"></path></svg>
            </div>
          </div>
        </div>
        <div>
          <h6>{{ i18n.t('footer.paymentTypes') }}</h6>
          <div style="font-size: 14px; opacity: 0.85;">
            {{ i18n.t('footer.paymentLead') }}
            <a routerLink="/contact" style="color: var(--color-accent);">{{ i18n.t('footer.paymentLink') }}</a>.
          </div>
          <div class="pay-pills">
            <span class="pay-pill" title="Visa"><svg viewBox="0 0 46 15" height="15" aria-label="Visa"><text x="0" y="13" font-family="Arial, sans-serif" font-size="15" font-weight="800" font-style="italic" letter-spacing="0.5" fill="#1A1F71">VISA</text></svg></span>
            <span class="pay-pill" title="MasterCard"><svg viewBox="0 0 40 22" height="20" aria-label="MasterCard"><circle cx="14" cy="11" r="10" fill="#EB001B"/><circle cx="26" cy="11" r="10" fill="#F79E1B"/><path d="M20 2.5a10 10 0 0 1 0 17 10 10 0 0 1 0-17z" fill="#FF5F00"/><text x="20" y="14" text-anchor="middle" font-family="Georgia, serif" font-size="7.5" font-style="italic" font-weight="700" fill="#fff">MasterCard</text></svg></span>
            <span class="pay-pill" title="ArCa"><svg viewBox="0 0 40 15" height="15" aria-label="ArCa"><text x="0" y="13" font-family="Arial, sans-serif" font-size="15" font-weight="800" font-style="italic" fill="#223E99">Ar</text><text x="19" y="13" font-family="Arial, sans-serif" font-size="15" font-weight="800" font-style="italic" fill="#E31E24">Ca</text></svg></span>
            <span class="pay-pill" title="Telcell Wallet"><svg viewBox="0 0 46 18" height="17" aria-label="Telcell Wallet"><text x="0" y="8" font-family="Arial, sans-serif" font-size="8" font-weight="800" fill="#F57C20">TELCELL</text><text x="0" y="17" font-family="Arial, sans-serif" font-size="7" font-weight="700" letter-spacing="1.2" fill="#F5A623">WALLET</text></svg></span>
            <span class="pay-pill" title="Idram"><svg viewBox="0 0 50 18" height="17" aria-label="Idram"><circle cx="8" cy="9" r="7" fill="none" stroke="#F26522" stroke-width="2.6"/><circle cx="8" cy="9" r="2.2" fill="#F26522"/><text x="19" y="14" font-family="Arial, sans-serif" font-size="13" font-weight="800" fill="#3A3A3A">idram</text></svg></span>
            <span class="pay-pill" title="American Express"><svg viewBox="0 0 48 17" height="17" aria-label="American Express"><rect width="48" height="17" rx="2" fill="#2E77BC"/><text x="24" y="7.5" text-anchor="middle" font-family="Arial, sans-serif" font-size="5.4" font-weight="800" fill="#fff">AMERICAN</text><text x="24" y="14" text-anchor="middle" font-family="Arial, sans-serif" font-size="5.4" font-weight="800" fill="#fff">EXPRESS</text></svg></span>
            <span class="pay-pill" title="MyAmeria"><svg viewBox="0 0 74 14" height="14" aria-label="MyAmeria"><path d="M0 12 5 1l5 11H7L5 7.4 3 12z" fill="#0DB14B"/><text x="13" y="11.5" font-family="Arial, sans-serif" font-size="11" font-weight="800" letter-spacing="0.4" fill="#2B2B2B">MYAMERIA</text></svg></span>
          </div>
        </div>
      </div>
      <div class="footer-bottom">{{ i18n.t('footer.copyright') }}</div>
    </div>

    </div>
  `,
})
export class StoreLayout implements OnInit {
  auth = inject(AuthStore);
  cart = inject(CartStore);
  theme = inject(ThemeStore);
  currency = inject(CurrencyStore);
  compare = inject(CompareStore);
  favorites = inject(FavoritesStore);
  i18n = inject(I18nService);
  private api = inject(ApiClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  private searchWrap = viewChild<ElementRef<HTMLElement>>('searchWrap');
  private userMenuWrap = viewChild<ElementRef<HTMLElement>>('userMenuWrap');

  categories = signal<Category[]>([]);
  searchTerm = '';

  searchOpen = signal(false);
  userMenuOpen = signal(false);
  recent = signal<string[]>(readRecentSearches());
  recommended = signal<string[]>([]);
  // Product names drawn once from the featured pool; Refresh reshuffles the sample.
  private recommendPool: string[] = [];

  // Mirrors the shop sidebar's own queryParamMap subscription, so the header chips
  // stay in sync (highlighted + toggle behavior) with whatever the URL says is selected.
  selectedCategoryIds = signal<number[]>([]);

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((qp) => {
      this.selectedCategoryIds.set(qp.getAll('categoryIds').map(Number).filter((n) => !Number.isNaN(n)));
    });
  }

  async ngOnInit(): Promise<void> {
    const [categories, featured] = await Promise.all([
      firstValueFrom(this.api.getCategories()),
      firstValueFrom(this.api.getFeatured(12)),
    ]);
    this.categories.set(categories);
    this.recommendPool = featured.map((p) => p.name);
    this.refreshRecommendations();
  }

  submitSearch(): void {
    const term = this.searchTerm.trim();
    if (term) {
      this.recent.update((r) => [term, ...r.filter((t) => t.toLowerCase() !== term.toLowerCase())].slice(0, 8));
      localStorage.setItem(RECENT_SEARCHES_KEY, JSON.stringify(this.recent()));
    }
    this.searchOpen.set(false);
    void this.router.navigate(['/shop'], { queryParams: { search: term || null } });
  }

  runSearch(term: string): void {
    this.searchTerm = term;
    this.submitSearch();
  }

  clearHistory(): void {
    this.recent.set([]);
    localStorage.removeItem(RECENT_SEARCHES_KEY);
  }

  refreshRecommendations(): void {
    const shuffled = [...this.recommendPool].sort(() => Math.random() - 0.5);
    this.recommended.set(shuffled.slice(0, 5));
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const searchWrap = this.searchWrap()?.nativeElement;
    if (this.searchOpen() && searchWrap && !searchWrap.contains(event.target as Node)) this.searchOpen.set(false);

    const userMenuWrap = this.userMenuWrap()?.nativeElement;
    if (this.userMenuOpen() && userMenuWrap && !userMenuWrap.contains(event.target as Node)) this.userMenuOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.searchOpen.set(false);
    this.userMenuOpen.set(false);
  }

  toggleCategory(id: number): void {
    const ids = this.selectedCategoryIds().includes(id)
      ? this.selectedCategoryIds().filter((x) => x !== id)
      : [...this.selectedCategoryIds(), id];
    void this.router.navigate(['/shop'], {
      queryParams: { categoryIds: ids.length ? ids : null },
      queryParamsHandling: 'merge',
    });
  }

  async logout(): Promise<void> {
    this.userMenuOpen.set(false);
    await this.auth.logout();
    await this.cart.reloadAsGuest();
    void this.router.navigateByUrl('/');
  }

  async setCurrency(code: Currency): Promise<void> {
    await this.currency.setCurrency(code);
    await this.cart.load();
  }
}
