import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { OrderDetail } from '../core/api.types';
import { AuthStore } from '../core/auth-store';
import { CartStore } from '../core/cart-store';
import { CurrencyStore } from '../core/currency-store';
import { I18nService } from '../core/i18n.service';
import { CHECKOUT_EMAIL_KEY } from './checkout';

@Component({
  selector: 'app-confirmation',
  imports: [DatePipe, RouterLink],
  template: `
    <div style="padding: 80px 32px; max-width: 560px; margin: 0 auto; text-align: center;">
      @if (loading()) {
        <div class="row" style="justify-content: center; padding: 40px;"><div class="spinner"></div></div>
        <p class="text-muted">{{ i18n.t('confirmation.confirmingPayment') }}</p>
      } @else if (order(); as o) {
        @if (paid()) {
          <div style="width: 64px; height: 64px; border-radius: 50%; border: 1px solid var(--color-accent); display: flex; align-items: center; justify-content: center; margin: 0 auto 22px;">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"></polyline></svg>
          </div>
          <h2>{{ i18n.t('confirmation.orderConfirmed') }}</h2>
          <p class="text-muted" style="margin-bottom: 24px;">
            {{ i18n.t('confirmation.orderConfirmedBody', { orderNumber: o.orderNumber }) }}
          </p>
        } @else {
          <div style="width: 64px; height: 64px; border-radius: 50%; border: 1px solid #ff8a8a; display: flex; align-items: center; justify-content: center; margin: 0 auto 22px; color: #ff8a8a;">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
          </div>
          <h2>{{ i18n.t('confirmation.paymentNotCompleted') }}</h2>
          <p class="text-muted" style="margin-bottom: 24px;">
            {{ i18n.t('confirmation.paymentNotCompletedBodyPrefix', { orderNumber: o.orderNumber }) }}
            {{ o.paymentFailureReason ? i18n.t('confirmation.paymentFailedReason', { reason: o.paymentFailureReason }) : i18n.t('confirmation.paymentNotCompletedGeneric') }}
            {{ i18n.t('confirmation.paymentNotCompletedBodySuffix') }}
          </p>
        }
        <div class="card elev-sm" style="text-align: left; gap: 10px; margin-bottom: 24px;">
          <div class="row" style="justify-content: space-between; font-size: 13px;"><span class="text-muted">{{ i18n.t('confirmation.orderTotal') }}</span><span>{{ currency.format(o.total, o.currency) }}</span></div>
          <div class="row" style="justify-content: space-between; font-size: 13px;"><span class="text-muted">{{ i18n.t('confirmation.placed') }}</span><span>{{ o.createdAt | date: 'MMM d, y, h:mm a' }}</span></div>
          <div class="row" style="justify-content: space-between; font-size: 13px;"><span class="text-muted">{{ i18n.t('confirmation.shippingTo') }}</span><span>{{ o.shipCity }}, {{ o.shipState }}</span></div>
          <div class="row" style="justify-content: space-between; font-size: 13px;"><span class="text-muted">{{ i18n.t('confirmation.status') }}</span><span class="tag tag-accent">{{ i18n.t('status.' + o.status) }}</span></div>
        </div>
        <div class="row" style="gap: 10px; justify-content: center;">
          @if (paid()) {
            @if (auth.isLoggedIn()) { <a class="btn btn-primary" routerLink="/account/orders">{{ i18n.t('confirmation.viewOrders') }}</a> }
            <a class="btn btn-secondary" routerLink="/">{{ i18n.t('confirmation.continueShopping') }}</a>
          } @else {
            <a class="btn btn-primary" routerLink="/checkout">{{ i18n.t('confirmation.tryAgain') }}</a>
            <a class="btn btn-secondary" routerLink="/cart">{{ i18n.t('confirmation.backToCart') }}</a>
          }
        </div>
      } @else {
        <h2>{{ i18n.t('confirmation.orderNotFound') }}</h2>
        <p class="text-muted" style="margin-bottom: 24px;">{{ i18n.t('confirmation.orderNotFoundBody') }}</p>
        <a class="btn btn-primary" target="_blank" routerLink="/">{{ i18n.t('confirmation.backToStore') }}</a>
      }
    </div>
  `,
})
export class ConfirmationPage implements OnInit {
  private api = inject(ApiClient);
  private route = inject(ActivatedRoute);
  private cart = inject(CartStore);
  auth = inject(AuthStore);
  currency = inject(CurrencyStore);
  i18n = inject(I18nService);

  loading = signal(true);
  order = signal<OrderDetail | null>(null);
  paid = signal(false);

  async ngOnInit(): Promise<void> {
    const orderNumber = this.route.snapshot.paramMap.get('orderNumber')!;
    const email = sessionStorage.getItem(CHECKOUT_EMAIL_KEY) ?? undefined;

    // The gateway callback normally settles the order before redirecting here,
    // but poll briefly in case the shopper beat the bank's processing.
    for (let attempt = 0; attempt < 5; attempt++) {
      try {
        const order = await firstValueFrom(this.api.getOrder(orderNumber, email));
        this.order.set(order);
        if (order.status !== 'PendingPayment') break;
      } catch {
        break;
      }
      await new Promise((r) => setTimeout(r, 2000));
    }

    const o = this.order();
    this.paid.set(o !== null && o.paidAt !== null);
    this.loading.set(false);
    if (this.paid()) void this.cart.load(); // server cleared it on payment
  }
}
