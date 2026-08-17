import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from './api-client';

const STORAGE_KEY = 'volt.currency';
const FALLBACK: Currency = 'AMD';

export type Currency = 'USD' | 'EUR' | 'AMD';

/**
 * Intl's currency formatting shows AMD as the "AMD" ISO code rather than the ֏ sign, and puts
 * symbols before the amount — this storefront shows every currency's symbol after the number
 * instead (e.g. "1,499.00 $"), so amounts are formatted as plain numbers here and the symbol is
 * appended by hand.
 */
const SYMBOLS: Record<Currency, string> = { USD: '$', EUR: '€', AMD: '֏' };

/**
 * Prices coming straight from the catalog (products, categories, admin product list/form) are
 * always in the store's base currency (USD) — convert() turns those into the shopper's chosen
 * display currency. Prices coming back from the cart/checkout/order endpoints are ALREADY in
 * whatever currency that cart or order is set to (converted + frozen server-side) — format those
 * directly with their own `currency` field instead of calling convert() again.
 */
@Injectable({ providedIn: 'root' })
export class CurrencyStore {
  private api = inject(ApiClient);

  readonly currency = signal<Currency>(readStored());
  readonly supported = signal<Currency[]>(['USD']);
  /** The store's base/settlement currency — admin analytics revenue is always normalized to this. */
  readonly base = signal<Currency>(FALLBACK);
  private rates = signal<Record<string, number>>({ USD: 1 });

  async init(): Promise<void> {
    try {
      const cfg = await firstValueFrom(this.api.getConfig());
      this.supported.set(cfg.supportedCurrencies as Currency[]);
      this.base.set(cfg.currency as Currency);
      this.rates.set(cfg.rates);
      if (!cfg.supportedCurrencies.includes(this.currency())) this.currency.set(FALLBACK);
    } catch {
      // keep the USD-only fallback — the storefront still works, just without conversion
    }
  }

  /** Converts an amount priced in the base currency (USD) into the currently selected currency. */
  convert(baseAmount: number): number {
    const rate = this.rates()[this.currency()] ?? 1;
    return Math.round(baseAmount * rate * 100) / 100;
  }

  format(amount: number, currencyCode?: string, fractionDigits = 2): string {
    const code = (currencyCode ?? this.currency()) as Currency;
    const number = new Intl.NumberFormat('en-US', {
      minimumFractionDigits: fractionDigits,
      maximumFractionDigits: fractionDigits,
    }).format(amount);
    const symbol = SYMBOLS[code] ?? code;
    return `${number} ${symbol}`;
  }

  /** Convert-and-format a base-currency (USD) amount in one call — the common case for catalog prices. */
  formatBase(baseAmount: number): string {
    return this.format(this.convert(baseAmount));
  }

  /**
   * Converts an amount priced in `fromCurrency` (not necessarily the base currency) into the
   * currently selected display currency. Needed for lists of past orders, which can each be
   * frozen in whatever currency was active when they were placed — without this, a table mixing
   * old USD/EUR orders with new AMD ones shows incomparable numbers side by side.
   */
  convertFrom(amount: number, fromCurrency: string): number {
    const fromRate = this.rates()[fromCurrency] ?? 1;
    return this.convert(amount / fromRate);
  }

  /** Convert-and-format an amount out of its own (possibly different) currency, in one call. */
  formatFrom(amount: number, fromCurrency: string): string {
    return this.format(this.convertFrom(amount, fromCurrency));
  }

  async setCurrency(code: Currency): Promise<void> {
    this.currency.set(code);
    localStorage.setItem(STORAGE_KEY, code);
    try {
      await firstValueFrom(this.api.setCartCurrency(code));
    } catch {
      // best effort — display conversion still works even if the cart sync fails
    }
  }
}

function readStored(): Currency {
  const v = localStorage.getItem(STORAGE_KEY);
  return v === 'USD' || v === 'EUR' ? v : FALLBACK;
}
