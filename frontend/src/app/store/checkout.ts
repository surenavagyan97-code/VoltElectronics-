import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { AuthStore } from '../core/auth-store';
import { CartStore, extractError } from '../core/cart-store';
import { CurrencyStore } from '../core/currency-store';
import { I18nService } from '../core/i18n.service';

/** Lets the guest confirmation page identify the order after the gateway redirect. */
export const CHECKOUT_EMAIL_KEY = 'volt.checkoutEmail';

@Component({
  selector: 'app-checkout',
  imports: [FormsModule, RouterLink],
  template: `
    <div style="padding: 32px; max-width: 1100px; margin: 0 auto;">
      <h2 style="margin-bottom: 24px;">{{ i18n.t('checkout.title') }}</h2>

      @if (cart.cart().items.length === 0) {
        <p class="text-muted">{{ i18n.t('checkout.emptyCart') }} <a routerLink="/shop">{{ i18n.t('checkout.backToShop') }}</a></p>
      } @else {
        <div style="display: grid; grid-template-columns: 1fr 340px; gap: 32px; align-items: flex-start;">
          <form class="col" style="gap: 28px;" (ngSubmit)="placeOrder()">
            <div>
              <h4 style="margin-bottom: 14px;">{{ i18n.t('checkout.contact') }}</h4>
              <div class="field"><label>{{ i18n.t('common.email') }}</label>
                <input class="input" type="email" name="email" [(ngModel)]="form.email" required autocomplete="email" placeholder="jordan@acme.com" />
              </div>
            </div>
            <div>
              <h4 style="margin-bottom: 14px;">{{ i18n.t('checkout.shippingAddress') }}</h4>
              <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px;">
                <div class="field"><label>{{ i18n.t('common.fullName') }}</label><input class="input" name="fullName" [(ngModel)]="form.fullName" required placeholder="Jordan Lee" /></div>
                <div class="field"><label>{{ i18n.t('checkout.companyOptional') }}</label><input class="input" name="company" [(ngModel)]="form.company" placeholder="Acme Corp" /></div>
                <div class="field" style="grid-column: span 2;"><label>{{ i18n.t('checkout.streetAddress') }}</label><input class="input" name="street" [(ngModel)]="form.street" required placeholder="500 Market St, Suite 200" /></div>
                <div class="field"><label>{{ i18n.t('checkout.city') }}</label><input class="input" name="city" [(ngModel)]="form.city" required placeholder="Yerevan" /></div>
                <div class="field"><label>{{ i18n.t('checkout.state') }}</label><input class="input" name="state" [(ngModel)]="form.state" required placeholder="Yerevan" /></div>
                <div class="field"><label>{{ i18n.t('checkout.zip') }}</label><input class="input" name="zip" [(ngModel)]="form.zip" required placeholder="0010" /></div>
                <div class="field"><label>{{ i18n.t('checkout.phone') }}</label><input class="input" name="phone" [(ngModel)]="form.phone" placeholder="+374 10 000000" /></div>
              </div>
            </div>
            <div>
              <h4 style="margin-bottom: 14px;">{{ i18n.t('checkout.payment') }}</h4>
              <div class="card" style="gap: 8px;">
                <div class="row" style="gap: 10px;">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--color-accent)" stroke-width="1.6"><rect x="1" y="4" width="22" height="16" rx="2"></rect><line x1="1" y1="10" x2="23" y2="10"></line></svg>
                  <span style="font-size: 14px;">{{ i18n.t('checkout.paymentMethod') }}</span>
                </div>
                <p class="text-muted" style="font-size: 13px; margin: 0;">
                  {{ i18n.t('checkout.paymentNote') }}
                </p>
              </div>
            </div>
            @if (error(); as err) { <div class="error-text">{{ err }}</div> }
          </form>

          <div class="card elev-sm" style="gap: 14px;">
            <div class="card-title">{{ i18n.t('cart.orderSummary') }}</div>
            @for (item of cart.cart().items; track item.productId) {
              <div class="row" style="justify-content: space-between; font-size: 13px;">
                <span>{{ item.name }} × {{ item.qty }}</span><span>{{ currency.format(item.lineTotal, cart.cart().currency) }}</span>
              </div>
            }
            <div class="hr" style="margin: 4px 0;"></div>
            <div class="row" style="justify-content: space-between; font-size: 14px;"><span class="text-muted">{{ i18n.t('cart.subtotal') }}</span><span>{{ currency.format(cart.cart().subtotal, cart.cart().currency) }}</span></div>
            <div class="row" style="justify-content: space-between; font-size: 14px;"><span class="text-muted">{{ i18n.t('checkout.shipping') }}</span><span>{{ currency.format(cart.cart().shipping, cart.cart().currency) }}</span></div>
            <div class="row" style="justify-content: space-between; font-size: 14px;"><span class="text-muted">{{ i18n.t('checkout.tax') }}</span><span>{{ currency.format(cart.cart().tax, cart.cart().currency) }}</span></div>
            <div class="row" style="justify-content: space-between; font-family: var(--font-heading); font-size: 18px;"><span>{{ i18n.t('cart.total') }}</span><span>{{ currency.format(cart.cart().total, cart.cart().currency) }}</span></div>
            <button class="btn btn-primary btn-block" [disabled]="busy()" (click)="placeOrder()">
              {{ busy() ? i18n.t('checkout.redirecting') : i18n.t('checkout.placeOrder') }}
            </button>
            <a class="btn btn-ghost btn-block" routerLink="/cart">{{ i18n.t('checkout.backToCart') }}</a>
          </div>
        </div>
      }
    </div>
  `,
})
export class CheckoutPage {
  private api = inject(ApiClient);
  cart = inject(CartStore);
  currency = inject(CurrencyStore);
  i18n = inject(I18nService);

  form = {
    email: inject(AuthStore).user()?.email ?? '',
    fullName: inject(AuthStore).user()?.fullName ?? '',
    company: '',
    street: '',
    city: '',
    state: '',
    zip: '',
    phone: '',
  };

  busy = signal(false);
  error = signal<string | null>(null);

  async placeOrder(): Promise<void> {
    const f = this.form;
    if (!f.email || !f.fullName || !f.street || !f.city || !f.state || !f.zip) {
      this.error.set(this.i18n.t('checkout.requiredFieldsError'));
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    try {
      const res = await firstValueFrom(this.api.checkout({
        email: f.email, fullName: f.fullName, company: f.company || null,
        street: f.street, city: f.city, state: f.state, zip: f.zip, phone: f.phone || null,
      }));
      sessionStorage.setItem(CHECKOUT_EMAIL_KEY, f.email);
      // Hand the shopper to the gateway's hosted pay page.
      window.location.href = res.paymentUrl;
    } catch (e) {
      this.error.set(extractError(e));
      this.busy.set(false);
    }
  }
}
