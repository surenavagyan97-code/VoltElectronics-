import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CartStore } from './core/cart-store';
import { CurrencyStore } from './core/currency-store';
import { ThemeStore } from './core/theme-store';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App implements OnInit {
  private cart = inject(CartStore);
  private theme = inject(ThemeStore);
  private currency = inject(CurrencyStore);

  async ngOnInit(): Promise<void> {
    await Promise.all([this.cart.load(), this.currency.init()]);
    // The cart's currency lives server-side and only changes when setCurrency() is called
    // explicitly (e.g. via the nav dropdown). If the locally-remembered preference doesn't
    // match what the cart actually has — a fresh guest cart defaults to USD, or the
    // preference was set on another device — push it now so displayed prices (converted
    // client-side from the preference) and the real cart/checkout total never disagree.
    if (this.cart.cart().currency !== this.currency.currency()) {
      await this.currency.setCurrency(this.currency.currency());
      await this.cart.load();
    }
  }
}
