import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from './api-client';
import { Cart } from './api.types';

const GUEST_ID_KEY = 'volt.cartId';
const EMPTY_CART: Cart = { id: '', items: [], count: 0, subtotal: 0, shipping: 0, tax: 0, total: 0 };

@Injectable({ providedIn: 'root' })
export class CartStore {
  private api = inject(ApiClient);

  private cartSignal = signal<Cart>(EMPTY_CART);
  readonly cart = this.cartSignal.asReadonly();
  readonly count = computed(() => this.cartSignal().count);

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  /** Guest cart GUID sent as X-Cart-Id (created lazily, survives reloads). */
  static guestId(): string {
    let id = localStorage.getItem(GUEST_ID_KEY);
    if (!id) {
      id = crypto.randomUUID();
      localStorage.setItem(GUEST_ID_KEY, id);
    }
    return id;
  }

  async load(): Promise<void> {
    await this.run(() => firstValueFrom(this.api.getCart()));
  }

  async add(productId: number, qty = 1): Promise<boolean> {
    return this.run(() => firstValueFrom(this.api.addCartItem(productId, qty)));
  }

  async updateQty(productId: number, qty: number): Promise<boolean> {
    return this.run(() => firstValueFrom(this.api.updateCartItem(productId, qty)));
  }

  async remove(productId: number): Promise<boolean> {
    return this.run(() => firstValueFrom(this.api.removeCartItem(productId)));
  }

  async clear(): Promise<boolean> {
    return this.run(() => firstValueFrom(this.api.clearCart()));
  }

  /** After login: fold the guest cart into the user's cart and drop the guest id. */
  async mergeAfterLogin(): Promise<void> {
    try {
      const cart = await firstValueFrom(this.api.mergeCart());
      this.cartSignal.set(cart);
      localStorage.removeItem(GUEST_ID_KEY);
    } catch {
      await this.load();
    }
  }

  /** After logout: back to (possibly empty) guest cart. */
  async reloadAsGuest(): Promise<void> {
    await this.load();
  }

  private async run(action: () => Promise<Cart>): Promise<boolean> {
    this.busy.set(true);
    this.error.set(null);
    try {
      this.cartSignal.set(await action());
      return true;
    } catch (e: unknown) {
      this.error.set(extractError(e));
      return false;
    } finally {
      this.busy.set(false);
    }
  }
}

export function extractError(e: unknown): string {
  const err = e as { error?: { error?: string }; message?: string };
  return err?.error?.error ?? err?.message ?? 'Something went wrong.';
}
