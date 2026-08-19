import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, firstValueFrom } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiClient } from '../core/api-client';
import {
  AdminProductListItem, Category, Promotion, PromotionScope, PromotionType, SavePromotionRequest,
} from '../core/api.types';
import { extractError } from '../core/cart-store';
import { CurrencyStore } from '../core/currency-store';
import { I18nService } from '../core/i18n.service';

interface FormState {
  code: string;
  name: string;
  type: PromotionType;
  value: number | null;
  scope: PromotionScope;
  categoryId: number | null;
  productIds: number[];
  minSubtotal: number | null;
  maxDiscountAmount: number | null;
  maxRedemptions: number | null;
  startsAt: string;
  expiresAt: string;
  isActive: boolean;
}

function emptyForm(): FormState {
  return {
    code: '', name: '', type: 'Percentage', value: null, scope: 'Order',
    categoryId: null, productIds: [], minSubtotal: null, maxDiscountAmount: null, maxRedemptions: null,
    startsAt: '', expiresAt: '', isActive: true,
  };
}

/**
 * <input type="datetime-local"> both reads and writes a timezone-less "yyyy-MM-ddTHH:mm" string
 * representing wall-clock time in the browser's own timezone — never UTC. `new Date(local)`
 * already parses that string as local time (correct for saving), but converting the other way
 * needs the local getters, not a raw slice of the UTC ISO string the API returns.
 */
function toLocalInput(iso: string | null): string {
  if (!iso) return '';
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}
function toIso(local: string): string | null {
  return local ? new Date(local).toISOString() : null;
}

@Component({
  selector: 'app-admin-promotions',
  imports: [FormsModule, DatePipe],
  styles: `
    .table-scroll { overflow-x: auto; }
    .promo-form-grid { display: flex; flex-direction: column; gap: 14px; }
    .promo-row { display: flex; gap: 14px; }
    .promo-row > .field { flex: 1; min-width: 0; }
    @media (max-width: 640px) { .promo-row { flex-direction: column; } }
    .chip-list { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 8px; }
    .chip-remove { background: none; border: none; cursor: pointer; color: inherit; padding: 0; margin-left: 6px; font-size: 13px; line-height: 1; }
    .product-results {
      max-height: 180px; overflow-y: auto; margin-top: 6px;
      border: 1px solid var(--color-divider); border-radius: var(--radius-md);
    }
    .product-result-row {
      display: flex; align-items: center; justify-content: space-between;
      padding: 7px 10px; font-size: 13px; cursor: pointer; border: none; background: none; width: 100%; text-align: left;
    }
    .product-result-row:hover { background: color-mix(in srgb, var(--color-accent) 10%, transparent); }
  `,
  template: `
    <div class="row" style="justify-content: space-between; margin-bottom: 20px; gap: 12px; flex-wrap: wrap;">
      <h2 style="margin: 0;">{{ i18n.t('admin.promotions.title') }}</h2>
      @if (!formOpen()) {
        <button class="btn btn-primary" (click)="startCreate()">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
          {{ i18n.t('admin.promotions.add') }}
        </button>
      }
    </div>

    <p class="text-muted" style="margin-bottom: 16px; font-size: 13px;">{{ i18n.t('admin.promotions.hint') }}</p>

    @if (error(); as err) { <div class="error-text" style="margin-bottom: 10px;">{{ err }}</div> }

    @if (formOpen()) {
      <div class="card elev-sm" style="margin-bottom: 24px; gap: 16px;">
        <div class="card-kicker">{{ editingId() ? i18n.t('admin.promotions.editTitle') : i18n.t('admin.promotions.newTitle') }}</div>

        <div class="promo-form-grid">
          <div class="promo-row">
            <div class="field" style="margin: 0;"><label>{{ i18n.t('admin.promotions.code') }}</label>
              <input class="input" [(ngModel)]="form.code" [placeholder]="i18n.t('admin.promotions.codePlaceholder')" [disabled]="!!editingId()" />
            </div>
            <div class="field" style="margin: 0;"><label>{{ i18n.t('admin.promotions.name') }}</label>
              <input class="input" [(ngModel)]="form.name" [placeholder]="i18n.t('admin.promotions.namePlaceholder')" />
            </div>
          </div>

          <div class="promo-row">
            <div class="field" style="margin: 0;"><label>{{ i18n.t('admin.promotions.type') }}</label>
              <select class="input" [(ngModel)]="form.type">
                <option value="Percentage">{{ i18n.t('admin.promotions.type.percentage') }}</option>
                <option value="FixedAmount">{{ i18n.t('admin.promotions.type.fixedAmount') }}</option>
              </select>
            </div>
            <div class="field" style="margin: 0;"><label>{{ i18n.t('admin.promotions.value') }} {{ form.type === 'Percentage' ? '(%)' : '($)' }}</label>
              <input class="input" type="number" min="0" [max]="form.type === 'Percentage' ? 100 : null" step="0.01" [(ngModel)]="form.value" />
            </div>
          </div>

          <div class="promo-row">
            <div class="field" style="margin: 0;"><label>{{ i18n.t('admin.promotions.scope') }}</label>
              <select class="input" [(ngModel)]="form.scope">
                <option value="Order">{{ i18n.t('admin.promotions.scope.order') }}</option>
                <option value="Category">{{ i18n.t('admin.promotions.scope.category') }}</option>
                <option value="Product">{{ i18n.t('admin.promotions.scope.product') }}</option>
              </select>
            </div>
            @if (form.scope === 'Category') {
              <div class="field" style="margin: 0;"><label>{{ i18n.t('shop.category') }}</label>
                <select class="input" [(ngModel)]="form.categoryId">
                  <option [ngValue]="null">—</option>
                  @for (c of categories(); track c.id) { <option [ngValue]="c.id">{{ c.name }}</option> }
                </select>
              </div>
            }
          </div>

          @if (form.scope === 'Order' || form.type === 'Percentage') {
            <div class="promo-row">
              @if (form.scope === 'Order') {
                <div class="field" style="margin: 0;"><label>{{ i18n.t('admin.promotions.minSubtotal') }}</label>
                  <input class="input" type="number" min="0" step="0.01" [(ngModel)]="form.minSubtotal" [placeholder]="i18n.t('admin.promotions.noMinimum')" />
                </div>
              }
              @if (form.type === 'Percentage') {
                <div class="field" style="margin: 0;"><label>{{ i18n.t('admin.promotions.maxDiscount') }}</label>
                  <input class="input" type="number" min="0" step="0.01" [(ngModel)]="form.maxDiscountAmount" [placeholder]="i18n.t('admin.promotions.noCap')" />
                </div>
              }
            </div>
          }

          <div class="promo-row">
            <div class="field" style="margin: 0;"><label>{{ i18n.t('admin.promotions.maxRedemptions') }}</label>
              <input class="input" type="number" min="1" step="1" [(ngModel)]="form.maxRedemptions" [placeholder]="i18n.t('admin.promotions.unlimited')" />
            </div>
          </div>

          <div class="promo-row">
            <div class="field" style="margin: 0;"><label>{{ i18n.t('admin.promotions.startsAt') }}</label>
              <input class="input" type="datetime-local" [(ngModel)]="form.startsAt" />
            </div>
            <div class="field" style="margin: 0;"><label>{{ i18n.t('admin.promotions.expiresAt') }}</label>
              <input class="input" type="datetime-local" [(ngModel)]="form.expiresAt" />
            </div>
          </div>
        </div>

        @if (form.scope === 'Product') {
          <div class="field" style="margin: 0;">
            <label>{{ i18n.t('admin.promotions.products') }}</label>
            <input class="input" [placeholder]="i18n.t('admin.promotions.searchProducts')"
                   (input)="onProductSearch($any($event.target).value)" />
            @if (productResults().length > 0) {
              <div class="product-results">
                @for (p of productResults(); track p.id) {
                  <button type="button" class="product-result-row" (click)="addProduct(p)" [disabled]="form.productIds.includes(p.id)">
                    <span>{{ p.name }} <span class="text-muted">({{ p.sku }})</span></span>
                    @if (form.productIds.includes(p.id)) { <span class="text-muted">{{ i18n.t('admin.promotions.added') }}</span> }
                  </button>
                }
              </div>
            }
            <div class="chip-list">
              @for (id of form.productIds; track id) {
                <span class="tag tag-outline">
                  {{ productNames()[id] ?? ('#' + id) }}
                  <button type="button" class="chip-remove" (click)="removeProduct(id)" aria-label="Remove">×</button>
                </span>
              }
            </div>
          </div>
        }

        <label class="checkbox" style="font-size: 13px;">
          <input type="checkbox" [(ngModel)]="form.isActive" />
          <span class="box"><svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg></span>
          {{ i18n.t('admin.promotions.active') }}
        </label>

        @if (formError(); as err) { <div class="error-text" style="font-size: 13px;">{{ err }}</div> }

        <div class="row" style="gap: 10px;">
          <button class="btn btn-primary" [disabled]="busy()" (click)="save()">{{ busy() ? i18n.t('admin.form.saving') : i18n.t('common.save') }}</button>
          <button class="btn btn-secondary" (click)="cancelForm()">{{ i18n.t('common.cancel') }}</button>
        </div>
      </div>
    }

    <div class="table-scroll">
    <table class="table">
      <thead><tr>
        <th>{{ i18n.t('admin.promotions.table.code') }}</th>
        <th>{{ i18n.t('admin.promotions.table.target') }}</th>
        <th>{{ i18n.t('admin.promotions.table.discount') }}</th>
        <th>{{ i18n.t('admin.promotions.table.redemptions') }}</th>
        <th>{{ i18n.t('admin.promotions.table.window') }}</th>
        <th>{{ i18n.t('admin.promotions.table.active') }}</th>
        <th></th>
      </tr></thead>
      <tbody>
        @for (p of promotions(); track p.id) {
          <tr>
            <td style="white-space: nowrap;">
              @if (p.code) { <span class="tag tag-accent">{{ p.code }}</span> } @else { <span class="text-muted">{{ i18n.t('admin.promotions.automaticSale') }}</span> }
              @if (p.name) { <div class="text-muted" style="font-size: 12px; margin-top: 2px;">{{ p.name }}</div> }
            </td>
            <td style="white-space: nowrap;">
              @switch (p.scope) {
                @case ('Order') { {{ i18n.t('admin.promotions.scope.order') }} }
                @case ('Category') { {{ p.categoryName ?? i18n.t('admin.promotions.scope.category') }} }
                @case ('Product') { {{ i18n.t('admin.promotions.productCount', { count: p.productIds.length }) }} }
              }
            </td>
            <td style="white-space: nowrap;">{{ p.type === 'Percentage' ? p.value + '%' : currency.formatBase(p.value) }}</td>
            <td style="white-space: nowrap;">{{ p.redemptionCount }}{{ p.maxRedemptions ? ' / ' + p.maxRedemptions : '' }}</td>
            <td class="text-muted" style="white-space: nowrap; font-size: 12px;">
              @if (!p.startsAt && !p.expiresAt) { {{ i18n.t('admin.promotions.noWindow') }} }
              @else {
                {{ p.startsAt ? (p.startsAt | date: 'MMM d') : '…' }} – {{ p.expiresAt ? (p.expiresAt | date: 'MMM d') : '…' }}
              }
            </td>
            <td><span class="tag" [class]="p.isActive ? 'tag-accent' : 'tag-dim'">{{ p.isActive ? i18n.t('admin.form.statusActive') : i18n.t('admin.promotions.inactive') }}</span></td>
            <td>
              <div class="row" style="gap: 4px; justify-content: flex-end;">
                <button class="btn btn-icon btn-ghost" (click)="startEdit(p)" [attr.aria-label]="i18n.t('common.edit')">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M12 20h9"></path><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z"></path></svg>
                </button>
                <button class="btn btn-icon btn-ghost" (click)="remove(p)" [attr.aria-label]="i18n.t('common.delete')">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M3 6h18"></path><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"></path></svg>
                </button>
              </div>
            </td>
          </tr>
        } @empty {
          <tr><td colspan="7" class="text-muted" style="text-align: center; padding: 24px;">{{ i18n.t('admin.promotions.empty') }}</td></tr>
        }
      </tbody>
    </table>
    </div>
  `,
})
export class AdminPromotionsPage implements OnInit {
  private api = inject(ApiClient);
  currency = inject(CurrencyStore);
  i18n = inject(I18nService);

  promotions = signal<Promotion[]>([]);
  categories = signal<Category[]>([]);
  error = signal<string | null>(null);
  formError = signal<string | null>(null);
  busy = signal(false);
  formOpen = signal(false);
  editingId = signal<number | null>(null);

  form: FormState = emptyForm();

  productResults = signal<AdminProductListItem[]>([]);
  productNames = signal<Record<number, string>>({});
  private productSearch$ = new Subject<string>();

  constructor() {
    this.productSearch$.pipe(debounceTime(250), takeUntilDestroyed()).subscribe((term) => void this.searchProducts(term));
  }

  async ngOnInit(): Promise<void> {
    await Promise.all([this.load(), this.loadCategories()]);
  }

  startCreate(): void {
    this.form = emptyForm();
    this.editingId.set(null);
    this.formError.set(null);
    this.productResults.set([]);
    this.formOpen.set(true);
  }

  async startEdit(p: Promotion): Promise<void> {
    this.form = {
      code: p.code ?? '', name: p.name ?? '', type: p.type, value: p.value, scope: p.scope,
      categoryId: p.categoryId, productIds: [...p.productIds],
      minSubtotal: p.minSubtotal, maxDiscountAmount: p.maxDiscountAmount, maxRedemptions: p.maxRedemptions,
      startsAt: toLocalInput(p.startsAt), expiresAt: toLocalInput(p.expiresAt), isActive: p.isActive,
    };
    this.editingId.set(p.id);
    this.formError.set(null);
    this.productResults.set([]);
    this.formOpen.set(true);

    if (p.productIds.length > 0) {
      try {
        const products = await firstValueFrom(this.api.getProductsByIds(p.productIds));
        this.productNames.update((names) => ({ ...names, ...Object.fromEntries(products.map((pr) => [pr.id, pr.name])) }));
      } catch {
        // Best effort — chips fall back to "#id" if names can't be resolved.
      }
    }
  }

  cancelForm(): void {
    this.formOpen.set(false);
  }

  onProductSearch(term: string): void {
    this.productSearch$.next(term);
  }

  private async searchProducts(term: string): Promise<void> {
    if (!term.trim()) {
      this.productResults.set([]);
      return;
    }
    const result = await firstValueFrom(this.api.adminGetProducts(1, 10, term.trim()));
    this.productResults.set(result.items);
  }

  addProduct(p: AdminProductListItem): void {
    if (this.form.productIds.includes(p.id)) return;
    this.form.productIds = [...this.form.productIds, p.id];
    this.productNames.update((names) => ({ ...names, [p.id]: p.name }));
  }

  removeProduct(id: number): void {
    this.form.productIds = this.form.productIds.filter((x) => x !== id);
  }

  async save(): Promise<void> {
    if (!this.form.value || this.form.value <= 0) {
      this.formError.set(this.i18n.t('admin.promotions.valueRequiredError'));
      return;
    }
    if (this.form.scope === 'Category' && !this.form.categoryId) {
      this.formError.set(this.i18n.t('admin.promotions.categoryRequiredError'));
      return;
    }
    if (this.form.scope === 'Product' && this.form.productIds.length === 0) {
      this.formError.set(this.i18n.t('admin.promotions.productsRequiredError'));
      return;
    }

    const request: SavePromotionRequest = {
      code: this.form.code.trim() || null,
      name: this.form.name.trim() || null,
      type: this.form.type,
      value: this.form.value,
      scope: this.form.scope,
      categoryId: this.form.scope === 'Category' ? this.form.categoryId : null,
      productIds: this.form.scope === 'Product' ? this.form.productIds : [],
      minSubtotal: this.form.scope === 'Order' ? this.form.minSubtotal : null,
      maxDiscountAmount: this.form.type === 'Percentage' ? this.form.maxDiscountAmount : null,
      maxRedemptions: this.form.maxRedemptions,
      startsAt: toIso(this.form.startsAt),
      expiresAt: toIso(this.form.expiresAt),
      isActive: this.form.isActive,
    };

    this.busy.set(true);
    this.formError.set(null);
    try {
      const id = this.editingId();
      if (id) await firstValueFrom(this.api.adminUpdatePromotion(id, request));
      else await firstValueFrom(this.api.adminCreatePromotion(request));
      this.formOpen.set(false);
      await this.load();
    } catch (e) {
      this.formError.set(extractError(e));
    } finally {
      this.busy.set(false);
    }
  }

  async remove(p: Promotion): Promise<void> {
    if (!confirm(this.i18n.t('admin.promotions.deleteConfirm', { label: p.code ?? p.name ?? ('#' + p.id) }))) return;
    try {
      await firstValueFrom(this.api.adminDeletePromotion(p.id));
      await this.load();
    } catch (e) {
      this.error.set(extractError(e));
    }
  }

  private async load(): Promise<void> {
    this.error.set(null);
    try {
      this.promotions.set(await firstValueFrom(this.api.adminGetPromotions()));
    } catch (e) {
      this.error.set(extractError(e));
    }
  }

  private async loadCategories(): Promise<void> {
    this.categories.set(await firstValueFrom(this.api.adminGetCategories()));
  }
}
