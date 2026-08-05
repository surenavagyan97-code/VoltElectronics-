import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, firstValueFrom } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiClient } from '../core/api-client';
import { AdminProductListItem, PagedResult } from '../core/api.types';
import { extractError } from '../core/cart-store';

@Component({
  selector: 'app-admin-products',
  imports: [CurrencyPipe, RouterLink],
  template: `
    <div class="row" style="justify-content: space-between; margin-bottom: 20px;">
      <h2 style="margin: 0;">Products</h2>
      <a class="btn btn-primary" routerLink="/admin/products/new">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
        Add product
      </a>
    </div>
    <div class="row" style="gap: 10px; margin-bottom: 16px;">
      <div class="field" style="margin: 0; width: 260px;">
        <input class="input" placeholder="Search products" (input)="search$.next($any($event.target).value)" />
      </div>
    </div>

    @if (error(); as err) { <div class="error-text" style="margin-bottom: 10px;">{{ err }}</div> }

    <table class="table">
      <thead><tr><th></th><th>Name</th><th>SKU</th><th>Category</th><th>Price</th><th>Stock</th><th>Status</th><th></th></tr></thead>
      <tbody>
        @for (p of result()?.items ?? []; track p.id) {
          <tr>
            <td>
              <div class="ph" style="width: 40px; height: 40px; font-size: 8px;">
                @if (p.imageUrl) { <img [src]="p.imageUrl" [alt]="p.name" /> } @else { img }
              </div>
            </td>
            <td>{{ p.name }}</td>
            <td class="text-muted">{{ p.sku }}</td>
            <td>{{ p.category }}</td>
            <td>{{ p.price | currency }}</td>
            <td><span class="tag" [class]="p.stock < 20 ? 'tag-accent' : 'tag-neutral'">{{ p.stock }} units</span></td>
            <td><span class="tag" [class]="p.status === 'Active' ? 'tag-accent' : p.status === 'Draft' ? 'tag-outline' : 'tag-dim'">{{ p.status }}</span></td>
            <td>
              <div class="row" style="gap: 4px;">
                <a class="btn btn-icon btn-ghost" [routerLink]="['/admin/products', p.id]" aria-label="Edit">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M12 20h9"></path><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z"></path></svg>
                </a>
                <button class="btn btn-icon btn-ghost" (click)="confirmDelete.set(p)" aria-label="Delete">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M3 6h18"></path><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"></path></svg>
                </button>
              </div>
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

    @if (confirmDelete(); as p) {
      <div class="dialog-backdrop" (click)="confirmDelete.set(null)">
        <div class="dialog" (click)="$event.stopPropagation()">
          <div class="dialog-title">Delete “{{ p.name }}”?</div>
          <div class="dialog-body">Products with order history are archived instead of deleted, so past orders stay intact.</div>
          <div class="dialog-actions">
            <button class="btn btn-secondary" (click)="confirmDelete.set(null)">Cancel</button>
            <button class="btn btn-primary" (click)="doDelete(p)">Delete</button>
          </div>
        </div>
      </div>
    }
  `,
})
export class AdminProductsPage implements OnInit {
  private api = inject(ApiClient);

  result = signal<PagedResult<AdminProductListItem> | null>(null);
  page = signal(1);
  search = signal('');
  error = signal<string | null>(null);
  confirmDelete = signal<AdminProductListItem | null>(null);

  search$ = new Subject<string>();

  constructor() {
    this.search$.pipe(debounceTime(300), takeUntilDestroyed()).subscribe((term) => {
      this.search.set(term);
      this.page.set(1);
      void this.load();
    });
  }

  ngOnInit(): void { void this.load(); }

  totalPages(): number {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1;
  }

  goPage(page: number): void {
    this.page.set(page);
    void this.load();
  }

  async doDelete(p: AdminProductListItem): Promise<void> {
    this.confirmDelete.set(null);
    try {
      await firstValueFrom(this.api.adminDeleteProduct(p.id));
      await this.load();
    } catch (e) {
      this.error.set(extractError(e));
    }
  }

  private async load(): Promise<void> {
    this.result.set(await firstValueFrom(this.api.adminGetProducts(this.page(), 20, this.search() || undefined)));
  }
}
