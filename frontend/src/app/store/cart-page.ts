import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CartStore } from '../core/cart-store';
import { CurrencyStore } from '../core/currency-store';
import { I18nService } from '../core/i18n.service';

@Component({
  selector: 'app-cart-page',
  imports: [RouterLink, FormsModule],
  styles: `
    .cart-layout { display: grid; grid-template-columns: 1fr 340px; gap: 32px; margin-top: 20px; align-items: flex-start; }
    @media (max-width: 760px) {
      .cart-layout { grid-template-columns: 1fr; }
    }
  `,
  template: `
    <div style="padding: 32px; max-width: 1100px; margin: 0 auto;">
      <h2>{{ i18n.t('cart.title') }} <span class="text-muted" style="font-size: 18px;">{{ i18n.t('cart.itemsCount', { count: cart.count() }) }}</span></h2>

      @if (cart.cart().items.length === 0) {
        <div style="padding: 40px 0;">
          <p class="text-muted">{{ i18n.t('cart.empty') }}</p>
          <a class="btn btn-primary" routerLink="/shop">{{ i18n.t('cart.browseCatalog') }}</a>
        </div>
      } @else {
        <div class="cart-layout">
          <div class="col" style="gap: 0; min-width: 0;">
            @for (item of cart.cart().items; track item.productId) {
              <div class="row" style="gap: 18px; padding: 18px 0; border-bottom: 1px solid var(--color-divider); flex-wrap: wrap;">
                <a [routerLink]="['/product', item.slug]" class="ph" style="width: 88px; height: 88px; text-decoration: none; flex: none;">
                  @if (item.imageUrl) { <img [src]="item.imageUrl" [alt]="item.name" /> } @else { {{ item.name }} }
                </a>
                <div class="col" style="flex: 1; gap: 4px; min-width: 160px;">
                  <a [routerLink]="['/product', item.slug]" class="card-title" style="font-size: 15px; color: inherit; text-decoration: none;">{{ item.name }}</a>
                  <div class="card-meta">{{ item.category }} · {{ currency.format(item.price, cart.cart().currency) }} {{ i18n.t('cart.each') }}</div>
                  <button class="btn btn-ghost" style="padding: 0; font-size: 12px; align-self: flex-start; margin-top: 4px;"
                          (click)="cart.remove(item.productId)">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M3 6h18"></path><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"></path></svg>
                    {{ i18n.t('cart.remove') }}
                  </button>
                </div>
                <div class="seg" style="flex: none;">
                  <button class="seg-opt" style="padding: 6px 10px; background: none; border-top: none; border-right: none; border-bottom: none; color: inherit; font: inherit;"
                          [disabled]="cart.busy()" (click)="cart.updateQty(item.productId, item.qty - 1)">−</button>
                  <div class="seg-opt" style="padding: 6px 14px;">{{ item.qty }}</div>
                  <button class="seg-opt" style="padding: 6px 10px; background: none; border-top: none; border-right: none; border-bottom: none; color: inherit; font: inherit;"
                          [disabled]="cart.busy() || item.qty >= item.stock" (click)="cart.updateQty(item.productId, item.qty + 1)">+</button>
                </div>
                <div style="font-family: var(--font-heading); font-size: 16px; flex: none; text-align: right; white-space: nowrap; margin-left: auto;">{{ currency.format(item.lineTotal, cart.cart().currency) }}</div>
              </div>
            }
            @if (cart.error(); as err) {
              <div class="error-text" style="margin-top: 10px;">{{ err }}</div>
            }
            <a class="btn btn-ghost" style="align-self: flex-start; margin-top: 14px;" routerLink="/shop">{{ i18n.t('cart.continueShopping') }}</a>
          </div>

          <div class="card elev-sm" style="gap: 14px;">
            <div class="card-title">{{ i18n.t('cart.orderSummary') }}</div>

            @if (cart.cart().couponCode; as code) {
              <div class="row" style="justify-content: space-between; font-size: 13px; background: color-mix(in srgb, var(--color-accent) 12%, transparent); padding: 8px 10px; border-radius: var(--radius-md);">
                <span>{{ cart.cart().couponError ? code : i18n.t('cart.couponApplied', { code }) }}</span>
                <button type="button" class="btn btn-ghost" style="padding: 0; font-size: 12px;" [disabled]="cart.busy()" (click)="removeCoupon()">{{ i18n.t('cart.couponRemove') }}</button>
              </div>
            } @else {
              <div class="row" style="gap: 8px;">
                <input class="input" style="flex: 1;" [placeholder]="i18n.t('cart.couponPlaceholder')"
                       [(ngModel)]="couponInput" [ngModelOptions]="{ standalone: true }" (keyup.enter)="applyCoupon()" />
                <button type="button" class="btn btn-secondary" [disabled]="cart.busy() || !couponInput.trim()" (click)="applyCoupon()">{{ i18n.t('cart.couponApply') }}</button>
              </div>
            }
            @if (cart.cart().couponError; as err) {
              <div class="error-text" style="font-size: 12px;">{{ err }}</div>
            }

            <div class="row" style="justify-content: space-between; font-size: 14px;"><span class="text-muted">{{ i18n.t('cart.subtotal') }}</span><span>{{ currency.format(cart.cart().subtotal, cart.cart().currency) }}</span></div>
            @if (cart.cart().discount > 0) {
              <div class="row" style="justify-content: space-between; font-size: 14px; color: var(--color-accent);"><span>{{ i18n.t('cart.discount') }}</span><span>−{{ currency.format(cart.cart().discount, cart.cart().currency) }}</span></div>
            }
            <div class="row" style="justify-content: space-between; font-size: 14px;"><span class="text-muted">{{ i18n.t('cart.estimatedShipping') }}</span><span>{{ currency.format(cart.cart().shipping, cart.cart().currency) }}</span></div>
            <div class="row" style="justify-content: space-between; font-size: 14px;"><span class="text-muted">{{ i18n.t('cart.estimatedTax') }}</span><span>{{ currency.format(cart.cart().tax, cart.cart().currency) }}</span></div>
            <div class="hr" style="margin: 4px 0;"></div>
            <div class="row" style="justify-content: space-between; font-family: var(--font-heading); font-size: 18px;"><span>{{ i18n.t('cart.total') }}</span><span>{{ currency.format(cart.cart().total, cart.cart().currency) }}</span></div>
            <a class="btn btn-primary btn-block" routerLink="/checkout">{{ i18n.t('cart.proceedToCheckout') }}</a>
          </div>
        </div>
      }
    </div>
  `,
})
export class CartPage {
  cart = inject(CartStore);
  currency = inject(CurrencyStore);
  i18n = inject(I18nService);

  couponInput = '';

  async applyCoupon(): Promise<void> {
    if (!this.couponInput.trim()) return;
    if (await this.cart.applyCoupon(this.couponInput.trim())) this.couponInput = '';
  }

  async removeCoupon(): Promise<void> {
    await this.cart.removeCoupon();
  }
}
