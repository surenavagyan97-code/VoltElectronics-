import { DecimalPipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { ProductListItem } from '../core/api.types';
import { CartStore } from '../core/cart-store';
import { CompareStore } from '../core/compare-store';
import { FavoritesStore } from '../core/favorites-store';
import { CurrencyStore } from '../core/currency-store';
import { I18nService } from '../core/i18n.service';

@Component({
  selector: 'app-product-card',
  imports: [DecimalPipe],
  styles: `
    .fav-btn, .compare-btn { opacity: 0; transition: opacity 0.15s ease; }
    .card:hover .fav-btn, .card:hover .compare-btn,
    .card:focus-within .fav-btn, .card:focus-within .compare-btn,
    .fav-btn:focus-visible, .compare-btn:focus-visible,
    .fav-btn.is-favorite, .compare-btn.is-compare { opacity: 1; }
  `,
  template: `
    <div class="card elev-sm" style="cursor: pointer; height: 100%; position: relative;" (click)="open()">
      <button class="btn btn-icon btn-secondary compare-btn" [class.is-compare]="compare.has(product().id)"
              style="position: absolute; top: 10px; right: 44px; z-index: 1; background: var(--color-bg);"
              [disabled]="!compare.has(product().id) && compare.isFull()"
              (click)="toggleCompare($event)"
              [attr.aria-label]="i18n.t('compare.title')" [attr.aria-pressed]="compare.has(product().id)">
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none"
             [attr.stroke]="compare.has(product().id) ? 'var(--color-accent)' : 'currentColor'"
             stroke-width="1.8"><line x1="4" y1="20" x2="4" y2="10"></line><line x1="12" y1="20" x2="12" y2="4"></line><line x1="20" y1="20" x2="20" y2="14"></line></svg>
      </button>
      <button class="btn btn-icon btn-secondary fav-btn" [class.is-favorite]="favorites.has(product().id)"
              style="position: absolute; top: 10px; right: 10px; z-index: 1; background: var(--color-bg);"
              (click)="toggleFavorite($event)"
              [attr.aria-label]="i18n.t('fav.title')" [attr.aria-pressed]="favorites.has(product().id)">
        <svg width="15" height="15" viewBox="0 0 24 24"
             [attr.fill]="favorites.has(product().id) ? 'var(--color-accent)' : 'none'"
             [attr.stroke]="favorites.has(product().id) ? 'var(--color-accent)' : 'currentColor'"
             stroke-width="1.8"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path></svg>
      </button>
      <div class="ph" style="width: 100%;" [style.height.px]="imageHeight()">
        @if (product().imageUrl) {
          <img [src]="product().imageUrl" [alt]="product().name" />
        } @else {
          {{ product().name }}
        }
      </div>
      @if (product().badge) {
        <div class="tag tag-accent" style="align-self: flex-start;">{{ product().badge }}</div>
      }
      <div class="card-title">{{ product().name }}</div>
      <div class="card-meta">{{ product().category }} · ★ {{ product().rating | number: '1.1-1' }} · {{ product().stock }} {{ i18n.t('common.inStockSuffix') }}</div>
      <div class="row" style="justify-content: space-between; margin-top: 4px;">
        <div class="row" style="gap: 8px;">
          <div style="font-family: var(--font-heading); font-size: 16px;">{{ currency.formatBase(product().price) }}</div>
          @if (product().compareAtPrice) {
            <div class="text-muted" style="font-size: 12px; text-decoration: line-through;">{{ currency.formatBase(product().compareAtPrice!) }}</div>
          }
        </div>
        <button class="btn btn-icon btn-secondary" [disabled]="product().stock === 0 || cart.busy()"
                (click)="addToCart($event)" [attr.aria-label]="i18n.t('product.addToCart')">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="9" cy="21" r="1"></circle><circle cx="20" cy="21" r="1"></circle><path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path></svg>
        </button>
      </div>
    </div>
  `,
})
export class ProductCard {
  product = input.required<ProductListItem>();
  imageHeight = input(150);

  cart = inject(CartStore);
  currency = inject(CurrencyStore);
  compare = inject(CompareStore);
  favorites = inject(FavoritesStore);
  i18n = inject(I18nService);
  private router = inject(Router);

  toggleFavorite(event: Event): void {
    event.stopPropagation();
    this.favorites.toggle(this.product().id);
  }

  toggleCompare(event: Event): void {
    event.stopPropagation();
    this.compare.toggle(this.product().id);
  }

  open(): void {
    void this.router.navigate(['/product', this.product().slug]);
  }

  addToCart(event: Event): void {
    event.stopPropagation();
    void this.cart.add(this.product().id, 1);
  }
}
