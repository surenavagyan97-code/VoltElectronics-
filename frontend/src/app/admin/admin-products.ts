import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, firstValueFrom } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiClient } from '../core/api-client';
import { AdminProductListItem, ImportProductsResult, PagedResult } from '../core/api.types';
import { extractError } from '../core/cart-store';
import { CurrencyStore } from '../core/currency-store';
import { I18nService } from '../core/i18n.service';

@Component({
  selector: 'app-admin-products',
  imports: [RouterLink],
  styles: `
    .drop-zone {
      display: flex; flex-direction: column; align-items: center; gap: 6px;
      padding: 28px 16px; cursor: pointer; text-align: center;
      border: 1.5px dashed var(--color-divider); border-radius: 10px;
      color: var(--color-text);
      transition: border-color 0.15s, background 0.15s;
    }
    .drop-zone:hover, .drop-zone.drag-over { border-color: var(--color-accent); background: var(--color-section); }
    .link-btn {
      background: none; border: none; padding: 0; cursor: pointer;
      color: var(--color-accent); text-decoration: underline; font: inherit;
    }
  `,
  template: `
    <div class="row" style="justify-content: space-between; margin-bottom: 20px;">
      <h2 style="margin: 0;">{{ i18n.t('admin.products.title') }}</h2>
      <div class="row" style="gap: 8px;">
        <button class="btn btn-secondary" [disabled]="exporting()" (click)="exportExcel()">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
          {{ i18n.t('admin.products.export') }}
        </button>
        <button class="btn btn-secondary" (click)="openImport()">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="17 8 12 3 7 8"></polyline><line x1="12" y1="3" x2="12" y2="15"></line></svg>
          {{ i18n.t('admin.products.import') }}
        </button>
        <a class="btn btn-primary" routerLink="/admin/products/new">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
          {{ i18n.t('admin.products.addProduct') }}
        </a>
      </div>
    </div>

    @if (importResult(); as res) {
      <div class="card" style="margin-bottom: 16px; padding: 14px 16px;">
        <div class="row" style="justify-content: space-between;">
          <strong>{{ i18n.t('admin.products.importSummary', { created: res.created, updated: res.updated }) }}</strong>
          <button class="btn btn-icon btn-ghost" (click)="importResult.set(null)" aria-label="Dismiss">×</button>
        </div>
        @if (res.errors.length) {
          <div class="error-text" style="margin-top: 8px; font-size: 13px;">
            {{ i18n.t('admin.products.importErrors', { count: res.errors.length }) }}
            <ul style="margin: 6px 0 0; padding-left: 18px;">
              @for (err of res.errors; track err.rowNumber) {
                <li>{{ i18n.t('admin.products.importRowError', { row: err.rowNumber, error: err.error }) }}</li>
              }
            </ul>
          </div>
        }
      </div>
    }
    <div class="row" style="gap: 10px; margin-bottom: 16px;">
      <div class="field" style="margin: 0; width: 260px;">
        <input class="input" [placeholder]="i18n.t('admin.products.searchPlaceholder')" (input)="search$.next($any($event.target).value)" />
      </div>
    </div>

    @if (error(); as err) { <div class="error-text" style="margin-bottom: 10px;">{{ err }}</div> }

    <table class="table">
      <thead><tr><th></th><th>{{ i18n.t('admin.products.table.name') }}</th><th>{{ i18n.t('admin.products.table.sku') }}</th><th>{{ i18n.t('admin.products.table.category') }}</th><th>{{ i18n.t('admin.products.table.price') }}</th><th>{{ i18n.t('admin.products.table.stock') }}</th><th>{{ i18n.t('admin.products.table.status') }}</th><th></th></tr></thead>
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
            <td>{{ currency.formatBase(p.price) }}</td>
            <td><span class="tag" [class]="p.stock < 20 ? 'tag-accent' : 'tag-neutral'">{{ i18n.t('admin.products.units', { count: p.stock }) }}</span></td>
            <td><span class="tag" [class]="p.status === 'Active' ? 'tag-accent' : p.status === 'Draft' ? 'tag-outline' : 'tag-dim'">{{ statusLabel(p.status) }}</span></td>
            <td>
              <div class="row" style="gap: 4px;">
                <a class="btn btn-icon btn-ghost" [routerLink]="['/admin/products', p.id]" [attr.aria-label]="i18n.t('common.edit')">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M12 20h9"></path><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z"></path></svg>
                </a>
                <button class="btn btn-icon btn-ghost" (click)="confirmDelete.set(p)" [attr.aria-label]="i18n.t('common.delete')">
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
        <button class="btn btn-secondary" [disabled]="page() <= 1" (click)="goPage(page() - 1)">{{ i18n.t('common.prev') }}</button>
        <span class="text-muted" style="font-size: 13px;">{{ i18n.t('common.pageOf', { page: page(), total: totalPages() }) }}</span>
        <button class="btn btn-secondary" [disabled]="page() >= totalPages()" (click)="goPage(page() + 1)">{{ i18n.t('common.next') }}</button>
      </div>
    }

    @if (importOpen()) {
      <div class="dialog-backdrop" (click)="closeImport()">
        <div class="dialog" style="width: 440px;" (click)="$event.stopPropagation()">
          <div class="dialog-title">{{ i18n.t('admin.products.importTitle') }}</div>
          <div class="dialog-body">
            <label class="drop-zone" [class.drag-over]="dragOver()"
                   [style.pointer-events]="importing() ? 'none' : null"
                   (dragover)="$event.preventDefault(); dragOver.set(true)"
                   (dragleave)="dragOver.set(false)"
                   (drop)="onDrop($event)">
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="17 8 12 3 7 8"></polyline><line x1="12" y1="3" x2="12" y2="15"></line></svg>
              <strong>{{ importing() ? i18n.t('admin.products.importing') : i18n.t('admin.products.dropHint') }}</strong>
              @if (!importing()) { <span class="text-muted" style="font-size: 13px;">{{ i18n.t('admin.products.dropBrowse') }}</span> }
              <input type="file" accept=".xlsx" style="display: none;" (change)="importExcel($event)" />
            </label>
            <div class="text-muted" style="margin-top: 12px; font-size: 13px;">
              {{ i18n.t('admin.products.templateLead') }}
              <button class="link-btn" (click)="downloadTemplate()">{{ i18n.t('admin.products.templateLink') }}</button>
            </div>
            @if (error(); as err) { <div class="error-text" style="margin-top: 10px; font-size: 13px;">{{ err }}</div> }
          </div>
          <div class="dialog-actions">
            <button class="btn btn-secondary" (click)="closeImport()">{{ i18n.t('common.cancel') }}</button>
          </div>
        </div>
      </div>
    }

    @if (confirmDelete(); as p) {
      <div class="dialog-backdrop" (click)="confirmDelete.set(null)">
        <div class="dialog" (click)="$event.stopPropagation()">
          <div class="dialog-title">{{ i18n.t('admin.products.deleteConfirmTitle', { name: p.name }) }}</div>
          <div class="dialog-body">{{ i18n.t('admin.products.deleteConfirmBody') }}</div>
          <div class="dialog-actions">
            <button class="btn btn-secondary" (click)="confirmDelete.set(null)">{{ i18n.t('common.cancel') }}</button>
            <button class="btn btn-primary" (click)="doDelete(p)">{{ i18n.t('common.delete') }}</button>
          </div>
        </div>
      </div>
    }
  `,
})
export class AdminProductsPage implements OnInit {
  private api = inject(ApiClient);
  currency = inject(CurrencyStore);
  i18n = inject(I18nService);

  result = signal<PagedResult<AdminProductListItem> | null>(null);
  page = signal(1);
  search = signal('');
  error = signal<string | null>(null);
  confirmDelete = signal<AdminProductListItem | null>(null);
  exporting = signal(false);
  importing = signal(false);
  importOpen = signal(false);
  dragOver = signal(false);
  importResult = signal<ImportProductsResult | null>(null);

  search$ = new Subject<string>();

  constructor() {
    this.search$.pipe(debounceTime(300), takeUntilDestroyed()).subscribe((term) => {
      this.search.set(term);
      this.page.set(1);
      void this.load();
    });
  }

  ngOnInit(): void { void this.load(); }

  statusLabel(status: string): string {
    if (status === 'Active') return this.i18n.t('admin.form.statusActive');
    if (status === 'Draft') return this.i18n.t('admin.form.statusDraft');
    return this.i18n.t('admin.form.statusArchived');
  }

  totalPages(): number {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1;
  }

  goPage(page: number): void {
    this.page.set(page);
    void this.load();
  }

  async exportExcel(): Promise<void> {
    this.exporting.set(true);
    this.error.set(null);
    try {
      this.saveBlob(
        await firstValueFrom(this.api.adminExportProducts()),
        `products-${new Date().toISOString().slice(0, 10)}.xlsx`,
      );
    } catch (e) {
      this.error.set(extractError(e));
    } finally {
      this.exporting.set(false);
    }
  }

  async downloadTemplate(): Promise<void> {
    this.error.set(null);
    try {
      this.saveBlob(
        await firstValueFrom(this.api.adminDownloadImportTemplate()),
        'products-import-template.xlsx',
      );
    } catch (e) {
      this.error.set(extractError(e));
    }
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }

  openImport(): void {
    this.error.set(null);
    this.importResult.set(null);
    this.dragOver.set(false);
    this.importOpen.set(true);
  }

  closeImport(): void {
    if (this.importing()) return;
    this.importOpen.set(false);
  }

  importExcel(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) void this.importFile(file);
    input.value = '';
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) void this.importFile(file);
  }

  private async importFile(file: File): Promise<void> {
    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      this.error.set(this.i18n.t('admin.products.onlyXlsx'));
      return;
    }
    this.importing.set(true);
    this.error.set(null);
    try {
      this.importResult.set(await firstValueFrom(this.api.adminImportProducts(file)));
      this.importOpen.set(false);
      this.page.set(1);
      await this.load();
    } catch (e) {
      this.error.set(extractError(e));
    } finally {
      this.importing.set(false);
    }
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
