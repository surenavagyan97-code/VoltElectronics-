import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { AdminOrderListItem, AdminOrderStats, Courier, PagedResult } from '../core/api.types';
import { extractError } from '../core/cart-store';
import { CurrencyStore } from '../core/currency-store';
import { I18nService } from '../core/i18n.service';
import { statusTagClass } from '../store/status-tag';

const STATUSES = ['PendingPayment', 'Processing', 'Shipped', 'Delivered', 'Cancelled'];

@Component({
  selector: 'app-admin-orders',
  imports: [DatePipe],
  template: `
    <h2 style="margin-bottom: 20px;">{{ i18n.t('admin.orders.title') }}</h2>

    @if (stats(); as s) {
      <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 14px; margin-bottom: 24px;">
        <div class="card elev-sm"><div class="card-kicker">{{ i18n.t('admin.orders.stats.total') }}</div><div style="font-family: var(--font-heading); font-size: 26px;">{{ s.total }}</div></div>
        <div class="card elev-sm"><div class="card-kicker">{{ i18n.t('admin.orders.stats.processing') }}</div><div style="font-family: var(--font-heading); font-size: 26px;">{{ s.processing }}</div></div>
        <div class="card elev-sm"><div class="card-kicker">{{ i18n.t('admin.orders.stats.shipped') }}</div><div style="font-family: var(--font-heading); font-size: 26px;">{{ s.shipped }}</div></div>
        <div class="card elev-sm"><div class="card-kicker">{{ i18n.t('admin.orders.stats.delivered') }}</div><div style="font-family: var(--font-heading); font-size: 26px;">{{ s.delivered }}</div></div>
      </div>
    }

    <div class="row" style="gap: 12px; margin-bottom: 16px; flex-wrap: wrap;">
      <div class="seg" style="width: fit-content;">
        <div class="seg-opt" [class.seg-active]="statusFilter() === ''" (click)="setStatus('')">{{ i18n.t('admin.orders.filterAll') }}</div>
        @for (s of statuses; track s) {
          <div class="seg-opt" [class.seg-active]="statusFilter() === s" (click)="setStatus(s)">{{ i18n.t('status.' + s) }}</div>
        }
      </div>
      <div class="field" style="margin: 0; width: 240px;">
        <input class="input" [placeholder]="i18n.t('admin.orders.searchPlaceholder')" (input)="onSearch($any($event.target).value)" />
      </div>
    </div>

    @if (error(); as err) { <div class="error-text" style="margin-bottom: 10px;">{{ err }}</div> }

    <table class="table">
      <thead><tr><th>{{ i18n.t('admin.orders.table.order') }}</th><th>{{ i18n.t('admin.orders.table.customer') }}</th><th>{{ i18n.t('admin.orders.table.email') }}</th><th>{{ i18n.t('admin.orders.table.date') }}</th><th>{{ i18n.t('admin.orders.table.items') }}</th><th>{{ i18n.t('admin.orders.table.total') }}</th><th>{{ i18n.t('admin.orders.table.status') }}</th><th>{{ i18n.t('admin.orders.table.courier') }}</th></tr></thead>
      <tbody>
        @for (o of result()?.items ?? []; track o.orderNumber) {
          <tr>
            <td>{{ o.orderNumber }}</td>
            <td>{{ o.customer }}</td>
            <td class="text-muted">{{ o.email }}</td>
            <td>{{ o.createdAt | date: 'MMM d, y' }}</td>
            <td>{{ o.itemCount }}</td>
            <td>{{ currency.format(o.total, o.currency) }}</td>
            <td>
              <select class="input" style="width: 150px; min-height: 30px; padding: 3px 8px; font-size: 12px;"
                      [value]="o.status" (change)="updateStatus(o, $any($event.target).value)">
                @for (s of statuses; track s) {
                  <option [value]="s" [selected]="s === o.status">{{ i18n.t('status.' + s) }}</option>
                }
              </select>
            </td>
            <td>
              <select class="input" style="width: 150px; min-height: 30px; padding: 3px 8px; font-size: 12px;"
                      (change)="assignCourier(o, $any($event.target).value)">
                <option value="" [selected]="!o.courierId">{{ i18n.t('admin.orders.unassigned') }}</option>
                @for (c of couriers(); track c.id) {
                  <option [value]="c.id" [selected]="c.id === o.courierId">{{ c.fullName }}</option>
                }
              </select>
            </td>
          </tr>
        }
      </tbody>
    </table>

    @if (totalPages() > 1) {
      <div class="row" style="gap: 8px; justify-content: center; margin-top: 20px;">
        <button class="btn btn-secondary" [disabled]="page() <= 1" (click)="goPage(page() - 1)">{{ i18n.t('common.prev') }}</button>
        <span class="text-muted" style="font-size: 13px;">{{ i18n.t('common.pageOf', { page: page(), total: totalPages() }) }}</span>
        <button class="btn btn-secondary" [disabled]="page() >= totalPages()" (click)="goPage(page() + 1)">{{ i18n.t('common.next') }}</button>
      </div>
    }
  `,
})
export class AdminOrdersPage implements OnInit {
  private api = inject(ApiClient);
  currency = inject(CurrencyStore);
  i18n = inject(I18nService);

  readonly statuses = STATUSES;
  statusClass = statusTagClass;

  stats = signal<AdminOrderStats | null>(null);
  couriers = signal<Courier[]>([]);
  result = signal<PagedResult<AdminOrderListItem> | null>(null);
  statusFilter = signal('');
  search = signal('');
  page = signal(1);
  error = signal<string | null>(null);

  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadStats(), this.load(), this.loadCouriers()]);
  }

  totalPages(): number {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1;
  }

  setStatus(status: string): void {
    this.statusFilter.set(status);
    this.page.set(1);
    void this.load();
  }

  onSearch(term: string): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.search.set(term);
      this.page.set(1);
      void this.load();
    }, 300);
  }

  goPage(page: number): void {
    this.page.set(page);
    void this.load();
  }

  async updateStatus(order: AdminOrderListItem, status: string): Promise<void> {
    try {
      await firstValueFrom(this.api.adminUpdateOrderStatus(order.orderNumber, status));
      await Promise.all([this.loadStats(), this.load()]);
    } catch (e) {
      this.error.set(extractError(e));
    }
  }

  async assignCourier(order: AdminOrderListItem, courierId: string): Promise<void> {
    try {
      await firstValueFrom(this.api.adminAssignOrderCourier(order.orderNumber, courierId || null));
      await this.load();
    } catch (e) {
      this.error.set(extractError(e));
    }
  }

  private async loadStats(): Promise<void> {
    this.stats.set(await firstValueFrom(this.api.adminGetOrderStats()));
  }

  private async loadCouriers(): Promise<void> {
    this.couriers.set(await firstValueFrom(this.api.adminGetCouriers()));
  }

  private async load(): Promise<void> {
    this.result.set(await firstValueFrom(this.api.adminGetOrders(
      this.page(), 20, this.statusFilter() || undefined, this.search() || undefined)));
  }
}
