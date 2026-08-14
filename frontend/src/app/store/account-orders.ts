import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { OrderSummary } from '../core/api.types';
import { AuthStore } from '../core/auth-store';
import { CurrencyStore } from '../core/currency-store';
import { I18nService } from '../core/i18n.service';
import { statusTagClass } from './status-tag';

@Component({
  selector: 'app-account-orders',
  imports: [DatePipe, RouterLink],
  styles: `
    .account-layout { padding: 32px; max-width: 1100px; margin: 0 auto; display: flex; gap: 32px; }
    .account-sidebar { width: 200px; flex: none; display: flex; flex-direction: column; gap: 4px; }
    .account-main { flex: 1; min-width: 0; }
    .table-scroll { overflow-x: auto; }
    @media (max-width: 640px) {
      .account-layout { flex-direction: column; padding: 20px 16px; gap: 20px; }
      .account-sidebar { width: 100%; }
    }
  `,
  template: `
    <div class="account-layout">
      <div class="account-sidebar">
        <div class="card" style="align-items: center; text-align: center; margin-bottom: 16px;">
          <div class="ph" style="width: 56px; height: 56px; border-radius: 50%;">{{ initials() }}</div>
          <div class="card-title" style="font-size: 14px;">{{ auth.user()?.fullName }}</div>
          <div class="card-meta" style="word-break: break-all;">{{ auth.user()?.email }}</div>
        </div>
        <a href="#" style="padding: 9px 12px; border-radius: var(--radius-md); background: color-mix(in srgb, var(--color-accent) 12%, transparent); color: var(--color-accent); font-size: 14px; text-decoration: none;">{{ i18n.t('account.orderHistory') }}</a>
      </div>
      <div class="account-main">
        <h3 style="margin-bottom: 18px;">{{ i18n.t('account.orderHistory') }}</h3>
        @if (loading()) {
          <div class="row" style="justify-content: center; padding: 40px;"><div class="spinner"></div></div>
        } @else if (orders().length === 0) {
          <p class="text-muted">{{ i18n.t('account.noOrders') }} <a routerLink="/shop">{{ i18n.t('account.startShopping') }}</a></p>
        } @else {
          <div class="table-scroll">
            <table class="table">
              <thead><tr><th>{{ i18n.t('account.table.order') }}</th><th>{{ i18n.t('account.table.date') }}</th><th>{{ i18n.t('account.table.items') }}</th><th>{{ i18n.t('account.table.total') }}</th><th>{{ i18n.t('account.table.status') }}</th></tr></thead>
              <tbody>
                @for (o of orders(); track o.orderNumber) {
                  <tr>
                    <td><a [routerLink]="['/confirmation', o.orderNumber]" style="text-decoration: none; white-space: nowrap;">{{ o.orderNumber }}</a></td>
                    <td style="white-space: nowrap;">{{ o.createdAt | date: 'MMM d, y' }}</td>
                    <td>{{ o.itemCount }}</td>
                    <td style="white-space: nowrap;">{{ currency.format(o.total, o.currency) }}</td>
                    <td><span class="tag" [class]="statusClass(o.status)">{{ i18n.t('status.' + o.status) }}</span></td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    </div>
  `,
})
export class AccountOrdersPage implements OnInit {
  private api = inject(ApiClient);
  auth = inject(AuthStore);
  currency = inject(CurrencyStore);
  i18n = inject(I18nService);

  loading = signal(true);
  orders = signal<OrderSummary[]>([]);

  statusClass = statusTagClass;

  initials(): string {
    return (this.auth.user()?.fullName ?? '')
      .split(' ')
      .map((w) => w[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  async ngOnInit(): Promise<void> {
    try {
      this.orders.set(await firstValueFrom(this.api.getMyOrders()));
    } finally {
      this.loading.set(false);
    }
  }
}
