import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { ProductDetail } from '../core/api.types';
import { COMPARE_MAX, CompareStore } from '../core/compare-store';
import { CurrencyStore } from '../core/currency-store';
import { I18nService } from '../core/i18n.service';

interface CompareRow {
  label: string;
  values: (string | null)[];
}

@Component({
  selector: 'app-compare',
  imports: [RouterLink],
  styles: `
    .compare-scroll { overflow-x: auto; }
    .compare-table { border-collapse: collapse; width: 100%; }
    .compare-table th, .compare-table td {
      padding: 12px 16px; text-align: left; vertical-align: top; font-size: 13px;
      border-bottom: 1px solid var(--color-divider);
    }
    .compare-table th { min-width: 220px; vertical-align: top; }
    .compare-table .row-label {
      white-space: nowrap; color: var(--color-text); opacity: 0.65;
      position: sticky; left: 0; background: var(--color-bg);
    }
    .remove-btn {
      background: none; border: none; cursor: pointer; opacity: 0.55; color: inherit;
      font-size: 16px; line-height: 1; padding: 2px 4px; border-radius: 4px;
    }
    .remove-btn:hover { opacity: 1; background: color-mix(in srgb, var(--color-text) 8%, transparent); }
    .clear-link {
      background: none; border: none; cursor: pointer; color: var(--color-accent);
      font: inherit; font-size: 13px; padding: 0;
    }
  `,
  template: `
    <div style="padding: 32px;">
      <div class="row" style="justify-content: space-between; align-items: baseline; margin-bottom: 18px; flex-wrap: wrap; gap: 8px;">
        <h2 style="margin: 0;">{{ i18n.t('compare.title') }} <span class="text-muted" style="font-size: 14px;">({{ compare.count() }}/{{ max }})</span></h2>
        @if (products().length > 0) {
          <button class="clear-link" (click)="clearAll()">{{ i18n.t('compare.clearAll') }}</button>
        }
      </div>

      @if (loading()) {
        <div class="row" style="justify-content: center; padding: 60px;"><div class="spinner"></div></div>
      } @else if (products().length === 0) {
        <p class="text-muted" style="margin-bottom: 14px;">{{ i18n.t('compare.empty') }}</p>
        <a class="btn btn-primary" routerLink="/shop">{{ i18n.t('compare.browse') }}</a>
      } @else {
        <div class="compare-scroll">
          <table class="compare-table">
            <thead>
              <tr>
                <th class="row-label"></th>
                @for (p of products(); track p.id) {
                  <th>
                    <div class="col" style="gap: 8px;">
                      <div class="row" style="justify-content: flex-end;">
                        <button class="remove-btn" (click)="remove(p.id)" [attr.aria-label]="i18n.t('compare.remove')">✕</button>
                      </div>
                      <div class="ph" style="width: 100%; height: 120px;">
                        @if (p.images[0]; as img) {
                          <img [src]="img.cardUrl" [alt]="p.name" />
                        } @else {
                          {{ p.name }}
                        }
                      </div>
                      <a [routerLink]="['/product', p.slug]" style="font-weight: 600; text-decoration: none; color: inherit; font-size: 14px;">{{ p.name }}</a>
                      <div style="font-family: var(--font-heading); font-size: 16px;">{{ currency.formatBase(p.price) }}</div>
                    </div>
                  </th>
                }
              </tr>
            </thead>
            <tbody>
              @for (row of rows(); track row.label) {
                <tr>
                  <td class="row-label">{{ row.label }}</td>
                  @for (v of row.values; track $index) {
                    <td>{{ v ?? '—' }}</td>
                  }
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class ComparePage {
  compare = inject(CompareStore);
  currency = inject(CurrencyStore);
  i18n = inject(I18nService);
  private api = inject(ApiClient);

  readonly max = COMPARE_MAX;
  products = signal<ProductDetail[]>([]);
  loading = signal(true);

  // Specs shared by every compared product line up on the same row; each product's
  // own extra specs still get a row, just blank ("—") for the products that lack them.
  rows = computed<CompareRow[]>(() => {
    const products = this.products();
    const specNames: string[] = [];
    for (const p of products) {
      for (const spec of p.specs) {
        if (!specNames.includes(spec.name)) specNames.push(spec.name);
      }
    }
    return [
      { label: this.i18n.t('compare.category'), values: products.map((p) => p.category) },
      { label: this.i18n.t('compare.rating'), values: products.map((p) => `${p.rating.toFixed(1)} (${p.reviewCount})`) },
      {
        label: this.i18n.t('compare.availability'),
        values: products.map((p) =>
          p.stock > 0 ? `${p.stock} ${this.i18n.t('common.inStockSuffix')}` : this.i18n.t('product.outOfStock'),
        ),
      },
      ...specNames.map((name) => ({
        label: name,
        values: products.map((p) => p.specs.find((s) => s.name === name)?.value ?? null),
      })),
    ];
  });

  constructor() {
    // Re-fetches whenever the id list changes, so removing a product drops it live.
    effect(() => {
      const ids = this.compare.ids();
      void this.load(ids);
    });
  }

  private async load(ids: number[]): Promise<void> {
    if (ids.length === 0) {
      this.products.set([]);
      this.loading.set(false);
      return;
    }
    try {
      this.products.set(await firstValueFrom(this.api.getProductsForCompare(ids)));
    } finally {
      this.loading.set(false);
    }
  }

  remove(id: number): void {
    this.compare.remove(id);
  }

  clearAll(): void {
    this.compare.clear();
  }
}
