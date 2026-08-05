import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { AdminOrderListItem, AdminOrderStats, PagedResult } from '../core/api.types';
import { extractError } from '../core/cart-store';
import { statusTagClass } from '../store/status-tag';

const STATUSES = ['PendingPayment', 'Processing', 'Shipped', 'Delivered', 'Cancelled'];

@Component({
  selector: 'app-admin-orders',
  imports: [CurrencyPipe, DatePipe],
  template: `
    <h2 style="margin-bottom: 20px;">Orders</h2>

    @if (stats(); as s) {
      <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 14px; margin-bottom: 24px;">
        <div class="card elev-sm"><div class="card-kicker">Total orders</div><div style="font-family: var(--font-heading); font-size: 26px;">{{ s.total }}</div></div>
        <div class="card elev-sm"><div class="card-kicker">Processing</div><div style="font-family: var(--font-heading); font-size: 26px;">{{ s.processing }}</div></div>
        <div class="card elev-sm"><div class="card-kicker">Shipped</div><div style="font-family: var(--font-heading); font-size: 26px;">{{ s.shipped }}</div></div>
        <div class="card elev-sm"><div class="card-kicker">Delivered</div><div style="font-family: var(--font-heading); font-size: 26px;">{{ s.delivered }}</div></div>
      </div>
    }

    <div class="row" style="gap: 12px; margin-bottom: 16px; flex-wrap: wrap;">
      <div class="seg" style="width: fit-content;">
        <div class="seg-opt" [class.seg-active]="statusFilter() === ''" (click)="setStatus('')">All</div>
        @for (s of statuses; track s) {
          <div class="seg-opt" [class.seg-active]="statusFilter() === s" (click)="setStatus(s)">{{ s }}</div>
        }
      </div>
      <div class="field" style="margin: 0; width: 240px;">
        <input class="input" placeholder="Search order / customer / email" (input)="onSearch($any($event.target).value)" />
      </div>
    </div>

    @if (error(); as err) { <div class="error-text" style="margin-bottom: 10px;">{{ err }}</div> }

    <table class="table">
      <thead><tr><th>Order</th><th>Customer</th><th>Email</th><th>Date</th><th>Items</th><th>Total</th><th>Status</th></tr></thead>
      <tbody>
        @for (o of result()?.items ?? []; track o.orderNumber) {
          <tr>
            <td>{{ o.orderNumber }}</td>
            <td>{{ o.customer }}</td>
            <td class="text-muted">{{ o.email }}</td>
            <td>{{ o.createdAt | date: 'MMM d, y' }}</td>
            <td>{{ o.itemCount }}</td>
            <td>{{ o.total | currency }}</td>
            <td>
              <select class="input" style="width: 150px; min-height: 30px; padding: 3px 8px; font-size: 12px;"
                      [value]="o.status" (change)="updateStatus(o, $any($event.target).value)">
                @for (s of statuses; track s) {
                  <option [value]="s" [selected]="s === o.status">{{ s }}</option>
                }
              </select>
            </td>
          </tr>
        }
      </tbody>
    </table>

    @if (totalPages() > 1) {
      <div class="row" style="gap: 8px; justify-content: center; margin-top: 20px;">
        <button class="btn btn-secondary" [disabled]="page() <= 1" (click)="goPage(page() - 1)">← Prev</button>
        <span class="text-muted" style="font-size: 13px;">Page {{ page() }} of {{ totalPages() }}</span>
        <button class="btn btn-secondary" [disabled]="page() >= totalPages()" (click)="goPage(page() + 1)">Next →</button>
      </div>
    }
  `,
})
export class AdminOrdersPage implements OnInit {
  private api = inject(ApiClient);

  readonly statuses = STATUSES;
  statusClass = statusTagClass;

  stats = signal<AdminOrderStats | null>(null);
  result = signal<PagedResult<AdminOrderListItem> | null>(null);
  statusFilter = signal('');
  search = signal('');
  page = signal(1);
  error = signal<string | null>(null);

  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadStats(), this.load()]);
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

  private async loadStats(): Promise<void> {
    this.stats.set(await firstValueFrom(this.api.adminGetOrderStats()));
  }

  private async load(): Promise<void> {
    this.result.set(await firstValueFrom(this.api.adminGetOrders(
      this.page(), 20, this.statusFilter() || undefined, this.search() || undefined)));
  }
}
