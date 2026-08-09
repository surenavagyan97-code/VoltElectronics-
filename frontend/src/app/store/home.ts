import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { Category, ProductListItem } from '../core/api.types';
import { I18nService } from '../core/i18n.service';
import { ProductCard } from './product-card';

@Component({
  selector: 'app-home',
  imports: [RouterLink, ProductCard],
  template: `
    <div style="background: linear-gradient(135deg, var(--color-section), var(--color-section-glow)); padding: 64px 32px; display: flex; align-items: center; gap: 48px; flex-wrap: wrap;">
      <div style="max-width: 460px;">
        <div class="tag tag-outline" style="margin-bottom: 14px;">{{ i18n.t('home.hero.kicker') }}</div>
        <h1 style="font-size: 46px; margin-bottom: 14px;">{{ i18n.t('home.hero.title') }}</h1>
        <p style="opacity: 0.8; font-size: 15px; margin-bottom: 22px;">{{ i18n.t('home.hero.subtitle') }}</p>
        <div class="row" style="gap: 10px;">
          <a class="btn btn-primary" routerLink="/shop">{{ i18n.t('home.hero.browseCatalog') }}</a>
          <button class="btn btn-secondary">{{ i18n.t('home.hero.talkToSales') }}</button>
        </div>
      </div>
      <div class="ph" style="width: 380px; height: 260px;">{{ i18n.t('home.hero.imagePlaceholder') }}</div>
    </div>

    <div style="padding: 48px 32px;">
      <h3 style="margin-bottom: 18px;">{{ i18n.t('home.shopByCategory') }}</h3>
      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(130px, 1fr)); gap: 14px;">
        @for (cat of categories(); track cat.id) {
          <div class="card" style="align-items: center; text-align: center; cursor: pointer;" (click)="openCategory(cat)">
            <div class="ph" style="width: 100%; height: 64px;">{{ cat.name }}</div>
            <div class="card-title" style="font-size: 14px;">{{ cat.name }}</div>
            <div class="card-meta">{{ i18n.t('home.productsCount', { count: cat.productCount }) }}</div>
          </div>
        }
      </div>
    </div>

    <div style="padding: 0 32px 56px;">
      <div class="row" style="justify-content: space-between; margin-bottom: 18px;">
        <h3 style="margin: 0;">{{ i18n.t('home.featuredProducts') }}</h3>
        <a class="btn btn-ghost" routerLink="/shop">{{ i18n.t('home.viewAll') }}</a>
      </div>
      <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(230px, 1fr)); gap: 16px;">
        @for (p of featured(); track p.id) {
          <app-product-card [product]="p" [imageHeight]="140" />
        }
      </div>
    </div>
  `,
})
export class HomePage implements OnInit {
  private api = inject(ApiClient);
  private router = inject(Router);
  i18n = inject(I18nService);

  categories = signal<Category[]>([]);
  featured = signal<ProductListItem[]>([]);

  async ngOnInit(): Promise<void> {
    const [categories, featured] = await Promise.all([
      firstValueFrom(this.api.getCategories()),
      firstValueFrom(this.api.getFeatured(4)),
    ]);
    this.categories.set(categories);
    this.featured.set(featured);
  }

  openCategory(cat: Category): void {
    void this.router.navigate(['/shop'], { queryParams: { categoryIds: cat.id } });
  }
}
