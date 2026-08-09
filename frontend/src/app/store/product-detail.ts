import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { ProductDetail } from '../core/api.types';
import { CartStore } from '../core/cart-store';
import { ProductCard } from './product-card';

@Component({
  selector: 'app-product-detail',
  imports: [CurrencyPipe, DecimalPipe, RouterLink, ProductCard],
  template: `
    @if (product(); as p) {
      <div style="padding: 32px; max-width: 1200px; margin: 0 auto;">
        <div class="text-muted" style="font-size: 13px; margin-bottom: 18px;">
          <a routerLink="/shop" style="color: inherit; text-decoration: none;">Shop</a>
          / {{ p.category }} / {{ p.name }}
        </div>
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 48px;">
          <div class="col" style="gap: 12px;">
            <div class="ph" style="width: 100%; height: 380px;">
              @if (activeImage(); as img) {
                <img [src]="img" [alt]="p.name" />
              } @else {
                {{ p.name }} — product photo
              }
            </div>
            @if (p.images.length > 1) {
              <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px;">
                @for (img of p.images; track img.id) {
                  <div class="ph" style="height: 76px; cursor: pointer;"
                       [style.outline]="activeImage() === img.url ? '2px solid var(--color-accent)' : 'none'"
                       (click)="activeImage.set(img.url)">
                    <img [src]="img.thumbUrl" [alt]="p.name" />
                  </div>
                }
              </div>
            }
          </div>
          <div class="col" style="gap: 16px;">
            @if (p.badge) {
              <div class="tag tag-accent" style="align-self: flex-start;">{{ p.badge }}</div>
            }
            <h1 style="margin: 0;">{{ p.name }}</h1>
            <div class="row" style="gap: 10px;">
              <div style="color: var(--color-accent-300);">{{ stars() }}</div>
              <div class="text-muted" style="font-size: 13px;">{{ p.rating | number: '1.1-1' }} ({{ p.reviewCount }} reviews) · {{ p.stock }} in stock</div>
            </div>
            <div class="row" style="gap: 10px; align-items: baseline;">
              <div style="font-family: var(--font-heading); font-size: 30px;">{{ p.price | currency }}</div>
              @if (p.compareAtPrice) {
                <div class="text-muted" style="text-decoration: line-through; font-size: 15px;">{{ p.compareAtPrice | currency }}</div>
                <div class="tag tag-accent-2">Save {{ savePct() }}%</div>
              }
            </div>
            <div class="hr"></div>
            <p style="opacity: 0.85;">{{ p.description }}</p>
            <div class="col" style="gap: 6px;">
              @for (spec of p.specs; track spec.name; let last = $last) {
                <div class="row" style="justify-content: space-between; font-size: 13px; padding: 8px 0;"
                     [style.border-bottom]="last ? 'none' : '1px solid var(--color-divider)'">
                  <span class="text-muted">{{ spec.name }}</span><span>{{ spec.value }}</span>
                </div>
              }
            </div>
            <div class="row" style="gap: 12px; margin-top: 8px;">
              <div class="field" style="margin: 0; width: 90px;">
                <label>Qty</label>
                <input class="input" type="number" [value]="qty()" min="1" [max]="p.stock"
                       (input)="qty.set(+$any($event.target).value || 1)" />
              </div>
              <button class="btn btn-primary" style="flex: 1; height: 36px; align-self: flex-end;"
                      [disabled]="p.stock === 0 || cart.busy()" (click)="addToCart()">
                @if (p.stock === 0) { Out of stock } @else { Add to cart — {{ p.price * qty() | currency }} }
              </button>
            </div>
            @if (added()) {
              <div class="tag tag-accent" style="align-self: flex-start;">Added to cart ✓</div>
            }
            @if (cart.error(); as err) {
              <div class="error-text">{{ err }}</div>
            }
            <button class="btn btn-secondary btn-block">Request bulk quote</button>
          </div>
        </div>

        @if (p.related.length > 0) {
          <div style="margin-top: 56px;">
            <h3 style="margin-bottom: 18px;">You may also like</h3>
            <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(230px, 1fr)); gap: 16px;">
              @for (r of p.related; track r.id) {
                <app-product-card [product]="r" [imageHeight]="120" />
              }
            </div>
          </div>
        }
      </div>
    } @else if (notFound()) {
      <div style="padding: 80px 32px; text-align: center;">
        <h2>Product not found</h2>
        <a class="btn btn-primary" routerLink="/shop">Back to shop</a>
      </div>
    } @else {
      <div class="row" style="justify-content: center; padding: 80px;"><div class="spinner"></div></div>
    }
  `,
})
export class ProductDetailPage {
  private api = inject(ApiClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  cart = inject(CartStore);

  product = signal<ProductDetail | null>(null);
  notFound = signal(false);
  activeImage = signal<string | null>(null);
  qty = signal(1);
  added = signal(false);

  savePct = computed(() => {
    const p = this.product();
    return p?.compareAtPrice ? Math.round((1 - p.price / p.compareAtPrice) * 100) : 0;
  });

  stars = computed(() => {
    const rating = Math.round(this.product()?.rating ?? 0);
    return '★'.repeat(rating) + '☆'.repeat(5 - rating);
  });

  constructor() {
    // Re-load when navigating between related products (same component instance).
    this.route.paramMap.subscribe((params) => void this.load(params.get('slug')!));
  }

  private async load(slug: string): Promise<void> {
    this.product.set(null);
    this.notFound.set(false);
    this.added.set(false);
    this.qty.set(1);
    try {
      const product = await firstValueFrom(this.api.getProduct(slug));
      this.product.set(product);
      this.activeImage.set(product.images[0]?.url ?? null);
    } catch {
      this.notFound.set(true);
    }
  }

  async addToCart(): Promise<void> {
    const p = this.product();
    if (!p) return;
    this.added.set(false);
    if (await this.cart.add(p.id, this.qty())) this.added.set(true);
  }
}
