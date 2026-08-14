import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { ProductListItem } from '../core/api.types';
import { FavoritesStore } from '../core/favorites-store';
import { I18nService } from '../core/i18n.service';
import { ProductCard } from './product-card';

@Component({
  selector: 'app-favorites',
  imports: [RouterLink, ProductCard],
  template: `
    <div style="padding: 32px;">
      <h2 style="margin-bottom: 18px;">{{ i18n.t('fav.title') }} <span class="text-muted" style="font-size: 14px;">({{ favorites.count() }})</span></h2>

      @if (loading()) {
        <div class="row" style="justify-content: center; padding: 60px;"><div class="spinner"></div></div>
      } @else if (products().length === 0) {
        <p class="text-muted" style="margin-bottom: 14px;">{{ i18n.t('fav.empty') }}</p>
        <a class="btn btn-primary" routerLink="/shop">{{ i18n.t('fav.browse') }}</a>
      } @else {
        <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 16px;">
          @for (p of products(); track p.id) {
            <app-product-card [product]="p" />
          }
        </div>
      }
    </div>
  `,
})
export class FavoritesPage {
  favorites = inject(FavoritesStore);
  i18n = inject(I18nService);
  private api = inject(ApiClient);

  products = signal<ProductListItem[]>([]);
  loading = signal(true);

  constructor() {
    // Re-fetches whenever the id list changes, so un-hearting a card removes it live.
    effect(() => {
      const ids = this.favorites.ids();
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
      this.products.set(await firstValueFrom(this.api.getProductsByIds(ids)));
    } finally {
      this.loading.set(false);
    }
  }
}
