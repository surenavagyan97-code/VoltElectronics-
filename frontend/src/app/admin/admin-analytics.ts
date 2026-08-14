import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { Analytics } from '../core/api.types';
import { CurrencyStore } from '../core/currency-store';
import { I18nService } from '../core/i18n.service';

@Component({
  selector: 'app-admin-analytics',
  imports: [DatePipe, DecimalPipe],
  styles: `
    .table-scroll { overflow-x: auto; }
    .stat-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 14px; margin-bottom: 28px; }
    @media (max-width: 640px) {
      .stat-grid { grid-template-columns: repeat(2, 1fr); }
    }
  `,
  template: `
    <h2 style="margin-bottom: 20px;">{{ i18n.t('admin.analytics.title') }}</h2>

    @if (data(); as a) {
      <div class="stat-grid">
        <div class="card elev-sm">
          <div class="card-kicker">{{ i18n.t('admin.analytics.revenue30d') }}</div>
          <div style="font-family: var(--font-heading); font-size: 26px;">{{ currency.format(a.revenue30d, currency.base(), 0) }}</div>
          <div class="card-meta" [style.color]="a.revenueDeltaPct >= 0 ? 'var(--color-accent-300)' : '#ff8a8a'">
            {{ a.revenueDeltaPct >= 0 ? '↑' : '↓' }} {{ abs(a.revenueDeltaPct) | number: '1.1-1' }}% {{ i18n.t('admin.analytics.vsPrior') }}
          </div>
        </div>
        <div class="card elev-sm">
          <div class="card-kicker">{{ i18n.t('admin.analytics.orders') }}</div>
          <div style="font-family: var(--font-heading); font-size: 26px;">{{ a.orders30d }}</div>
          <div class="card-meta" [style.color]="a.ordersDeltaPct >= 0 ? 'var(--color-accent-300)' : '#ff8a8a'">
            {{ a.ordersDeltaPct >= 0 ? '↑' : '↓' }} {{ abs(a.ordersDeltaPct) | number: '1.1-1' }}%
          </div>
        </div>
        <div class="card elev-sm">
          <div class="card-kicker">{{ i18n.t('admin.analytics.avgOrderValue') }}</div>
          <div style="font-family: var(--font-heading); font-size: 26px;">{{ currency.format(a.averageOrderValue30d, currency.base()) }}</div>
          <div class="card-meta">{{ i18n.t('admin.analytics.last30Days') }}</div>
        </div>
        <div class="card elev-sm">
          <div class="card-kicker">{{ i18n.t('admin.analytics.lowStockAlerts') }}</div>
          <div style="font-family: var(--font-heading); font-size: 26px; color: var(--color-accent-300);">{{ a.lowStockCount }}</div>
          <div class="card-meta">{{ i18n.t('admin.analytics.belowUnits') }}</div>
        </div>
      </div>

      <div class="card elev-sm" style="margin-bottom: 24px;">
        <div class="card-title" style="margin-bottom: 14px;">{{ i18n.t('admin.analytics.revenueLast7Days') }}</div>
        <div class="row" style="align-items: flex-end; gap: 14px; height: 160px;">
          @for (day of a.revenueByDay7d; track day.day) {
            <div class="col" style="align-items: center; gap: 8px; flex: 1; height: 100%; justify-content: flex-end;">
              <div class="text-muted" style="font-size: 10px;">{{ currency.format(day.revenue, currency.base(), 0) }}</div>
              <div style="width: 100%; background: var(--color-accent-700); border-radius: 4px 4px 0 0; min-height: 2px;"
                   [style.height.%]="barHeight(day.revenue)"></div>
              <div class="text-muted" style="font-size: 11px;">{{ day.day | date: 'EEE' }}</div>
            </div>
          }
        </div>
      </div>

      <div class="card-title" style="margin-bottom: 12px;">{{ i18n.t('admin.analytics.topProducts') }}</div>
      <div class="table-scroll" style="margin-bottom: 28px;">
      <table class="table">
        <thead><tr><th>{{ i18n.t('admin.analytics.table.product') }}</th><th>{{ i18n.t('admin.analytics.table.unitsSold') }}</th><th>{{ i18n.t('admin.analytics.table.revenue') }}</th></tr></thead>
        <tbody>
          @for (p of a.topProducts; track p.productId) {
            <tr><td style="white-space: nowrap;">{{ p.name }}</td><td>{{ p.unitsSold }}</td><td style="white-space: nowrap;">{{ currency.format(p.revenue, currency.base(), 0) }}</td></tr>
          }
        </tbody>
      </table>
      </div>

      @if (a.lowStockProducts.length > 0) {
        <div class="card-title" style="margin-bottom: 12px;">{{ i18n.t('admin.analytics.lowStock') }}</div>
        <div class="table-scroll">
        <table class="table">
          <thead><tr><th>{{ i18n.t('admin.analytics.table.product') }}</th><th>{{ i18n.t('admin.products.table.sku') }}</th><th>{{ i18n.t('admin.products.table.category') }}</th><th>{{ i18n.t('admin.products.table.stock') }}</th></tr></thead>
          <tbody>
            @for (p of a.lowStockProducts; track p.id) {
              <tr>
                <td style="white-space: nowrap;">{{ p.name }}</td>
                <td class="text-muted" style="white-space: nowrap;">{{ p.sku }}</td>
                <td style="white-space: nowrap;">{{ p.category }}</td>
                <td><span class="tag tag-accent" style="white-space: nowrap;">{{ i18n.t('admin.products.units', { count: p.stock }) }}</span></td>
              </tr>
            }
          </tbody>
        </table>
        </div>
      }
    } @else {
      <div class="row" style="justify-content: center; padding: 60px;"><div class="spinner"></div></div>
    }
  `,
})
export class AdminAnalyticsPage implements OnInit {
  private api = inject(ApiClient);
  currency = inject(CurrencyStore);
  i18n = inject(I18nService);

  data = signal<Analytics | null>(null);

  private maxDayRevenue = computed(() =>
    Math.max(1, ...(this.data()?.revenueByDay7d.map((d) => d.revenue) ?? [1])));

  abs = Math.abs;

  barHeight(revenue: number): number {
    return Math.round((revenue / this.maxDayRevenue()) * 80);
  }

  async ngOnInit(): Promise<void> {
    this.data.set(await firstValueFrom(this.api.adminGetAnalytics()));
  }
}
