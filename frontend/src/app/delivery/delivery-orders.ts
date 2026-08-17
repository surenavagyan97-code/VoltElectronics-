import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { DeliveryOrder } from '../core/api.types';
import { extractError } from '../core/cart-store';
import { CurrencyStore } from '../core/currency-store';
import { I18nService } from '../core/i18n.service';
import { statusTagClass } from '../store/status-tag';

// The statuses a courier actually meets: assigned (Processing), on the road (Shipped), done.
const FILTERS = ['Processing', 'Shipped', 'Delivered'];

@Component({
  selector: 'app-delivery-orders',
  imports: [DatePipe],
  template: `
    <h2 style="margin-bottom: 20px;">{{ i18n.t('delivery.title') }}</h2>

    <div class="seg" style="width: fit-content; margin-bottom: 20px;">
      <div class="seg-opt" [class.seg-active]="statusFilter() === ''" (click)="setStatus('')">{{ i18n.t('delivery.filterAll') }}</div>
      @for (s of filters; track s) {
        <div class="seg-opt" [class.seg-active]="statusFilter() === s" (click)="setStatus(s)">{{ i18n.t('status.' + s) }}</div>
      }
    </div>

    @if (error(); as err) { <div class="error-text" style="margin-bottom: 10px;">{{ err }}</div> }

    @if (loaded() && orders().length === 0) {
      <div class="card elev-sm text-muted" style="text-align: center; padding: 40px;">{{ i18n.t('delivery.noOrders') }}</div>
    }

    <div class="col" style="gap: 14px;">
      @for (o of orders(); track o.orderNumber) {
        <div class="card elev-sm">
          <div class="row" style="justify-content: space-between; flex-wrap: wrap; gap: 8px; margin-bottom: 10px;">
            <div class="row" style="gap: 10px;">
              <strong>{{ o.orderNumber }}</strong>
              <span class="tag" [class]="statusClass(o.status)">{{ i18n.t('status.' + o.status) }}</span>
            </div>
            <span class="text-muted" style="font-size: 13px;">{{ o.createdAt | date: 'MMM d, y' }}</span>
          </div>

          <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 12px; margin-bottom: 12px;">
            <div>
              <div class="card-kicker">{{ i18n.t('delivery.deliverTo') }}</div>
              <div>{{ o.fullName }}</div>
              <div class="text-muted" style="font-size: 13px;">{{ o.street }}, {{ o.city }}, {{ o.state }} {{ o.zip }}</div>
              @if (o.phone) { <a [href]="'tel:' + o.phone" style="font-size: 13px;">{{ o.phone }}</a> }
            </div>
            <div>
              <div class="card-kicker">{{ i18n.t('delivery.items') }}</div>
              @for (item of o.items; track item.productName) {
                <div style="font-size: 13px;">{{ item.qty }} × {{ item.productName }}</div>
              }
            </div>
            <div>
              <div class="card-kicker">{{ i18n.t('delivery.toCollect') }}</div>
              <div style="font-family: var(--font-heading); font-size: 20px;">{{ currency.formatFrom(o.total, o.currency) }}</div>
            </div>
          </div>

          <div class="row" style="gap: 8px; justify-content: flex-end;">
            @if (o.status === 'Processing') {
              <button class="btn btn-primary" [disabled]="busy()" (click)="updateStatus(o, 'Shipped')">{{ i18n.t('delivery.markShipped') }}</button>
            }
            @if (o.status === 'Shipped') {
              <button class="btn btn-primary" [disabled]="busy()" (click)="updateStatus(o, 'Delivered')">{{ i18n.t('delivery.markDelivered') }}</button>
            }
          </div>
        </div>
      }
    </div>
  `,
})
export class DeliveryOrdersPage implements OnInit {
  private api = inject(ApiClient);
  currency = inject(CurrencyStore);
  i18n = inject(I18nService);

  readonly filters = FILTERS;
  statusClass = statusTagClass;

  orders = signal<DeliveryOrder[]>([]);
  statusFilter = signal('');
  loaded = signal(false);
  busy = signal(false);
  error = signal<string | null>(null);

  async ngOnInit(): Promise<void> { await this.load(); }

  setStatus(status: string): void {
    this.statusFilter.set(status);
    void this.load();
  }

  async updateStatus(order: DeliveryOrder, status: string): Promise<void> {
    this.busy.set(true);
    try {
      await firstValueFrom(this.api.deliveryUpdateOrderStatus(order.orderNumber, status));
      await this.load();
    } catch (e) {
      this.error.set(extractError(e));
    } finally {
      this.busy.set(false);
    }
  }

  private async load(): Promise<void> {
    try {
      this.orders.set(await firstValueFrom(this.api.deliveryGetOrders(this.statusFilter() || undefined)));
      this.error.set(null);
    } catch (e) {
      this.error.set(extractError(e));
    } finally {
      this.loaded.set(true);
    }
  }
}
