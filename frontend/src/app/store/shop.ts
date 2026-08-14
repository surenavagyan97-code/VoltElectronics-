import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, firstValueFrom } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiClient } from '../core/api-client';
import { Category, PagedResult, ProductListItem } from '../core/api.types';
import { I18nService } from '../core/i18n.service';
import { ProductCard } from './product-card';

const PRICE_BANDS = [
  { key: 'lt250', labelKey: 'shop.priceBand.lt250' },
  { key: '250-750', labelKey: 'shop.priceBand.250-750' },
  { key: '750-1500', labelKey: 'shop.priceBand.750-1500' },
  { key: 'gt1500', labelKey: 'shop.priceBand.gt1500' },
];

@Component({
  selector: 'app-shop',
  imports: [ProductCard],
  template: `
    <div style="display: flex; gap: 28px; padding: 32px; align-items: flex-start;">
      <div class="col" style="width: 220px; flex: none; gap: 22px;">
        <div>
          <h6 style="margin-bottom: 10px;">{{ i18n.t('shop.category') }}</h6>
          <div class="col" style="gap: 8px; max-height: 280px; overflow-y: auto; padding-right: 4px;">
            @for (cat of categories(); track cat.id) {
              <label class="checkbox" style="font-size: 13px;">
                <input type="checkbox" [checked]="selectedCategories().includes(cat.id)"
                       (change)="toggleCategory(cat.id)" />
                <span class="box">
                  <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>
                </span>
                {{ cat.name }} <span class="text-muted">({{ cat.productCount }})</span>
              </label>
            }
          </div>
        </div>
        <div>
          <h6 style="margin-bottom: 10px;">{{ i18n.t('shop.price') }}</h6>
          <div class="col" style="gap: 8px; font-size: 13px;">
            @for (band of priceBands; track band.key) {
              <label class="checkbox">
                <input type="checkbox" [checked]="selectedBands().includes(band.key)"
                       (change)="toggleBand(band.key)" />
                <span class="box">
                  <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>
                </span>
                {{ i18n.t(band.labelKey) }}
              </label>
            }
          </div>
        </div>
        <button class="btn btn-secondary btn-block" (click)="clearFilters()">{{ i18n.t('shop.clearFilters') }}</button>
      </div>

      <div style="flex: 1;">
        <div class="row" style="justify-content: space-between; margin-bottom: 18px; gap: 12px; flex-wrap: wrap;">
          <h3 style="margin: 0;">{{ i18n.t('shop.allProducts') }}</h3>
          <div class="row" style="gap: 10px;">
            <select class="input" style="width: 170px;" [value]="sort()" (change)="onSort($any($event.target).value)">
              <option value="featured">{{ i18n.t('shop.sort.featured') }}</option>
              <option value="price_asc">{{ i18n.t('shop.sort.priceAsc') }}</option>
              <option value="price_desc">{{ i18n.t('shop.sort.priceDesc') }}</option>
              <option value="rating">{{ i18n.t('shop.sort.topRated') }}</option>
            </select>
          </div>
        </div>

        @if (loading()) {
          <div class="row" style="justify-content: center; padding: 60px;"><div class="spinner"></div></div>
        } @else if ((result()?.items ?? []).length === 0) {
          <p class="text-muted" style="padding: 40px 0;">{{ i18n.t('shop.noProducts') }}</p>
        } @else {
          <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 16px;">
            @for (p of result()!.items; track p.id) {
              <app-product-card [product]="p" />
            }
          </div>
          @if (totalPages() > 1) {
            <div class="row" style="gap: 8px; justify-content: center; margin-top: 28px;">
              <button class="btn btn-secondary" [disabled]="page() <= 1" (click)="goPage(page() - 1)">{{ i18n.t('common.prev') }}</button>
              <span class="text-muted" style="font-size: 13px;">{{ i18n.t('common.pageOf', { page: page(), total: totalPages() }) }}</span>
              <button class="btn btn-secondary" [disabled]="page() >= totalPages()" (click)="goPage(page() + 1)">{{ i18n.t('common.next') }}</button>
            </div>
          }
        }
      </div>
    </div>
  `,
})
export class ShopPage implements OnInit {
  private api = inject(ApiClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  i18n = inject(I18nService);

  readonly priceBands = PRICE_BANDS;

  categories = signal<Category[]>([]);
  result = signal<PagedResult<ProductListItem> | null>(null);
  loading = signal(true);

  selectedCategories = signal<number[]>([]);
  selectedBands = signal<string[]>([]);
  search = signal('');
  sort = signal('featured');
  page = signal(1);

  private searchInput$ = new Subject<string>();

  constructor() {
    this.searchInput$.pipe(debounceTime(300), takeUntilDestroyed()).subscribe((term) => {
      this.search.set(term);
      this.page.set(1);
      void this.load();
    });

    // Subscribed (not snapshotted) so the header's search box and category chips keep working
    // when the shopper is already on /shop. Emits immediately, so this also does the first load.
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((qp) => {
      this.selectedCategories.set(qp.getAll('categoryIds').map(Number).filter((n) => !Number.isNaN(n)));
      this.selectedBands.set(qp.getAll('priceBands'));
      this.search.set(qp.get('search') ?? '');
      this.page.set(1);
      void this.load();
    });
  }

  async ngOnInit(): Promise<void> {
    this.categories.set(await firstValueFrom(this.api.getCategories()));
  }

  totalPages(): number {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1;
  }

  onSearch(term: string): void { this.searchInput$.next(term); }

  onSort(sort: string): void {
    this.sort.set(sort);
    this.page.set(1);
    void this.load();
  }

  // Sidebar filters navigate rather than touch the signals directly — the queryParamMap
  // subscription above is the single source of truth, so the URL always mirrors what's
  // selected (shareable/bookmarkable, and survives a refresh).
  toggleCategory(id: number): void {
    const ids = this.selectedCategories().includes(id)
      ? this.selectedCategories().filter((x) => x !== id)
      : [...this.selectedCategories(), id];
    void this.router.navigate([], {
      queryParams: { categoryIds: ids.length ? ids : null },
      queryParamsHandling: 'merge',
    });
  }

  toggleBand(key: string): void {
    const bands = this.selectedBands().includes(key)
      ? this.selectedBands().filter((b) => b !== key)
      : [...this.selectedBands(), key];
    void this.router.navigate([], {
      queryParams: { priceBands: bands.length ? bands : null },
      queryParamsHandling: 'merge',
    });
  }

  clearFilters(): void {
    void this.router.navigate([], { queryParams: {} });
  }

  goPage(page: number): void {
    this.page.set(page);
    void this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.result.set(await firstValueFrom(this.api.getProducts({
        page: this.page(),
        pageSize: 12,
        categoryIds: this.selectedCategories(),
        priceBands: this.selectedBands(),
        search: this.search() || undefined,
        sort: this.sort(),
      })));
    } finally {
      this.loading.set(false);
    }
  }
}
