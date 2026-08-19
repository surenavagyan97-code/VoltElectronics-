import { DecimalPipe } from '@angular/common';
import { Component, ElementRef, computed, effect, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { ProductDetail } from '../core/api.types';
import { CartStore } from '../core/cart-store';
import { CompareStore } from '../core/compare-store';
import { CurrencyStore } from '../core/currency-store';
import { I18nService } from '../core/i18n.service';
import { ProductCard } from './product-card';

@Component({
  selector: 'app-product-detail',
  imports: [DecimalPipe, RouterLink, ProductCard],
  styles: `
    .product-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 48px; }
    @media (max-width: 760px) {
      .product-grid { grid-template-columns: 1fr; gap: 24px; }
    }
    .carousel-main { position: relative; }
    .carousel-nav {
      position: absolute; top: 50%; transform: translateY(-50%);
      width: 36px; height: 36px; border-radius: 50%; border: none; cursor: pointer;
      display: flex; align-items: center; justify-content: center;
      background: color-mix(in srgb, var(--color-bg) 70%, transparent); color: var(--color-text);
      opacity: 0; transition: opacity 0.15s ease, background 0.15s ease;
    }
    .carousel-main:hover .carousel-nav, .carousel-nav:focus-visible { opacity: 1; }
    .carousel-nav:hover { background: color-mix(in srgb, var(--color-bg) 90%, transparent); }
    .carousel-prev { left: 10px; }
    .carousel-next { right: 10px; }
    .thumb-strip {
      display: flex; gap: 10px; overflow-x: auto; scrollbar-width: none;
    }
    .thumb-strip::-webkit-scrollbar { display: none; }
    .thumb-strip .thumb { flex: 0 0 76px; height: 76px; cursor: pointer; }
  `,
  template: `
    @if (product(); as p) {
      <div style="padding: 32px; max-width: 1200px; margin: 0 auto;">
        <div class="text-muted" style="font-size: 13px; margin-bottom: 18px;">
          <a routerLink="/shop" style="color: inherit; text-decoration: none;">{{ i18n.t('nav.shop') }}</a>
          / {{ p.category }} / {{ p.name }}
        </div>
        <div class="product-grid">
          <div class="col" style="gap: 12px;">
            <div class="ph carousel-main" style="width: 100%; height: 380px;">
              @if (p.images[activeIndex()]; as img) {
                <img [src]="img.url" [alt]="p.name" />
              } @else {
                {{ p.name }} — product photo
              }
              @if (p.images.length > 1) {
                <button type="button" class="carousel-nav carousel-prev" (click)="prevImage()" [attr.aria-label]="i18n.t('product.prevImage')">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="15 18 9 12 15 6"></polyline></svg>
                </button>
                <button type="button" class="carousel-nav carousel-next" (click)="nextImage()" [attr.aria-label]="i18n.t('product.nextImage')">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
                </button>
              }
            </div>
            @if (p.images.length > 1) {
              <div class="thumb-strip" #thumbStrip>
                @for (img of p.images; track img.id; let i = $index) {
                  <div class="ph thumb"
                       [style.outline]="activeIndex() === i ? '2px solid var(--color-accent)' : 'none'"
                       (click)="activeIndex.set(i)">
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
              <div class="text-muted" style="font-size: 13px;">{{ p.rating | number: '1.1-1' }} {{ i18n.t('product.reviews', { count: p.reviewCount }) }} · {{ p.stock }} {{ i18n.t('common.inStockSuffix') }}</div>
            </div>
            <div class="row" style="gap: 10px; align-items: baseline;">
              <div style="font-family: var(--font-heading); font-size: 30px;">{{ currency.formatBase(p.price) }}</div>
              @if (p.compareAtPrice) {
                <div class="text-muted" style="text-decoration: line-through; font-size: 15px;">{{ currency.formatBase(p.compareAtPrice) }}</div>
                <div class="tag tag-accent-2">{{ i18n.t('product.save', { pct: savePct() }) }}</div>
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
                <label>{{ i18n.t('product.qty') }}</label>
                <input class="input" type="number" [value]="qty()" min="1" [max]="p.stock"
                       (input)="qty.set(+$any($event.target).value || 1)" />
              </div>
              <button class="btn btn-primary" style="flex: 1; height: 36px; align-self: flex-end;"
                      [disabled]="p.stock === 0 || cart.busy()" (click)="addToCart()">
                @if (p.stock === 0) { {{ i18n.t('product.outOfStock') }} } @else { {{ i18n.t('product.addToCartWithPrice', { price: currency.formatBase(p.price * qty()) }) }} }
              </button>
            </div>
            @if (added()) {
              <div class="tag tag-accent" style="align-self: flex-start;">{{ i18n.t('product.addedToCart') }}</div>
            }
            @if (cart.error(); as err) {
              <div class="error-text">{{ err }}</div>
            }
            <button class="btn btn-secondary btn-block" [class.is-compare]="compare.has(p.id)"
                    [disabled]="!compare.has(p.id) && compare.isFull()" (click)="toggleCompare()">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none"
                   [attr.stroke]="compare.has(p.id) ? 'var(--color-accent)' : 'currentColor'"
                   stroke-width="1.8"><line x1="4" y1="20" x2="4" y2="10"></line><line x1="12" y1="20" x2="12" y2="4"></line><line x1="20" y1="20" x2="20" y2="14"></line></svg>
              {{ compare.has(p.id) ? i18n.t('compare.remove') : i18n.t('compare.add') }}
            </button>
            <button class="btn btn-secondary btn-block">{{ i18n.t('product.requestBulkQuote') }}</button>
          </div>
        </div>

        @if (p.related.length > 0) {
          <div style="margin-top: 56px;">
            <h3 style="margin-bottom: 18px;">{{ i18n.t('product.youMayAlsoLike') }}</h3>
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
        <h2>{{ i18n.t('product.notFound') }}</h2>
        <a class="btn btn-primary" routerLink="/shop">{{ i18n.t('product.backToShop') }}</a>
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
  compare = inject(CompareStore);
  currency = inject(CurrencyStore);
  i18n = inject(I18nService);

  product = signal<ProductDetail | null>(null);
  notFound = signal(false);
  activeIndex = signal(0);
  qty = signal(1);
  added = signal(false);

  private thumbStrip = viewChild<ElementRef<HTMLElement>>('thumbStrip');

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

    // Keep the active thumbnail scrolled into view as the arrows move through a strip
    // wider than its container — re-fires once the strip itself renders, too.
    effect(() => {
      const index = this.activeIndex();
      const strip = this.thumbStrip()?.nativeElement;
      const thumb = strip?.children[index] as HTMLElement | undefined;
      thumb?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
    });
  }

  private async load(slug: string): Promise<void> {
    this.product.set(null);
    this.notFound.set(false);
    this.added.set(false);
    this.qty.set(1);
    this.activeIndex.set(0);
    try {
      const product = await firstValueFrom(this.api.getProduct(slug));
      this.product.set(product);
    } catch {
      this.notFound.set(true);
    }
  }

  prevImage(): void {
    const count = this.product()?.images.length ?? 0;
    if (count > 0) this.activeIndex.update((i) => (i - 1 + count) % count);
  }

  nextImage(): void {
    const count = this.product()?.images.length ?? 0;
    if (count > 0) this.activeIndex.update((i) => (i + 1) % count);
  }

  async addToCart(): Promise<void> {
    const p = this.product();
    if (!p) return;
    this.added.set(false);
    if (await this.cart.add(p.id, this.qty())) this.added.set(true);
  }

  toggleCompare(): void {
    const p = this.product();
    if (p) this.compare.toggle(p.id);
  }
}
