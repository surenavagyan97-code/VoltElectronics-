import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { Category } from '../core/api.types';
import { extractError } from '../core/cart-store';
import { I18nService } from '../core/i18n.service';

@Component({
  selector: 'app-admin-categories',
  imports: [FormsModule],
  styles: `.table-scroll { overflow-x: auto; }`,
  template: `
    <div class="row" style="justify-content: space-between; margin-bottom: 20px; gap: 12px; flex-wrap: wrap;">
      <h2 style="margin: 0;">{{ i18n.t('admin.categories.title') }}</h2>
      <button class="btn btn-primary" (click)="startCreate()">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
        {{ i18n.t('admin.categories.addCategory') }}
      </button>
    </div>

    @if (error(); as err) { <div class="error-text" style="margin-bottom: 10px;">{{ err }}</div> }

    <div class="table-scroll">
    <table class="table">
      <thead><tr><th></th><th>{{ i18n.t('admin.categories.table.category') }}</th><th>{{ i18n.t('admin.categories.table.slug') }}</th><th>{{ i18n.t('admin.categories.table.products') }}</th><th></th></tr></thead>
      <tbody>
        @for (cat of categories(); track cat.id) {
          <tr>
            <td>
              <div style="position: relative; width: fit-content;">
                <label class="ph" style="width: 40px; height: 40px; font-size: 8px; cursor: pointer;"
                       [attr.aria-label]="i18n.t('admin.categories.uploadImage')">
                  @if (cat.imageUrl) { <img [src]="cat.imageUrl" [alt]="cat.name" /> } @else { img }
                  <input type="file" accept="image/*" style="display: none;" (change)="uploadImage(cat, $event)" />
                </label>
                @if (cat.imageUrl) {
                  <button class="btn btn-icon btn-secondary" style="position: absolute; top: -6px; right: -6px; width: 18px; height: 18px; font-size: 11px; background: var(--color-bg);"
                          (click)="removeImage(cat)" [attr.aria-label]="i18n.t('admin.categories.removeImage')">×</button>
                }
              </div>
            </td>
            <td>
              @if (editingId() === cat.id) {
                <input class="input" style="width: 220px;" [(ngModel)]="editName" (keyup.enter)="saveEdit(cat)" />
              } @else {
                {{ cat.name }}
              }
            </td>
            <td class="text-muted">{{ cat.slug }}</td>
            <td>{{ cat.productCount }}</td>
            <td>
              <div class="row" style="gap: 4px; justify-content: flex-end;">
                @if (editingId() === cat.id) {
                  <button class="btn btn-ghost" (click)="saveEdit(cat)">{{ i18n.t('common.save') }}</button>
                  <button class="btn btn-secondary" (click)="editingId.set(null)">{{ i18n.t('common.cancel') }}</button>
                } @else {
                  <button class="btn btn-icon btn-ghost" (click)="startEdit(cat)" [attr.aria-label]="i18n.t('common.edit')">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M12 20h9"></path><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z"></path></svg>
                  </button>
                  <button class="btn btn-icon btn-ghost" (click)="remove(cat)" [attr.aria-label]="i18n.t('common.delete')">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M3 6h18"></path><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"></path></svg>
                  </button>
                }
              </div>
            </td>
          </tr>
        }
        @if (creating()) {
          <tr>
            <td></td>
            <td><input class="input" style="width: 220px;" [placeholder]="i18n.t('admin.categories.namePlaceholder')" [(ngModel)]="newName" (keyup.enter)="saveCreate()" /></td>
            <td class="text-muted">—</td>
            <td>0</td>
            <td>
              <div class="row" style="gap: 4px; justify-content: flex-end;">
                <button class="btn btn-ghost" (click)="saveCreate()">{{ i18n.t('common.save') }}</button>
                <button class="btn btn-secondary" (click)="creating.set(false)">{{ i18n.t('common.cancel') }}</button>
              </div>
            </td>
          </tr>
        }
      </tbody>
    </table>
    </div>
  `,
})
export class AdminCategoriesPage implements OnInit {
  private api = inject(ApiClient);
  i18n = inject(I18nService);

  categories = signal<Category[]>([]);
  error = signal<string | null>(null);
  editingId = signal<number | null>(null);
  creating = signal(false);
  editName = '';
  newName = '';

  async ngOnInit(): Promise<void> { await this.load(); }

  startCreate(): void {
    this.newName = '';
    this.creating.set(true);
    this.editingId.set(null);
  }

  startEdit(cat: Category): void {
    this.editName = cat.name;
    this.editingId.set(cat.id);
    this.creating.set(false);
  }

  async saveCreate(): Promise<void> {
    if (!this.newName.trim()) return;
    try {
      await firstValueFrom(this.api.adminCreateCategory(this.newName.trim()));
      this.creating.set(false);
      await this.load();
    } catch (e) {
      this.error.set(extractError(e));
    }
  }

  async saveEdit(cat: Category): Promise<void> {
    if (!this.editName.trim()) return;
    try {
      await firstValueFrom(this.api.adminUpdateCategory(cat.id, this.editName.trim()));
      this.editingId.set(null);
      await this.load();
    } catch (e) {
      this.error.set(extractError(e));
    }
  }

  async uploadImage(cat: Category, event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    try {
      await firstValueFrom(this.api.adminUploadCategoryImage(cat.id, file));
      await this.load();
    } catch (e) {
      this.error.set(extractError(e));
    } finally {
      input.value = '';
    }
  }

  async removeImage(cat: Category): Promise<void> {
    try {
      await firstValueFrom(this.api.adminRemoveCategoryImage(cat.id));
      await this.load();
    } catch (e) {
      this.error.set(extractError(e));
    }
  }

  async remove(cat: Category): Promise<void> {
    try {
      await firstValueFrom(this.api.adminDeleteCategory(cat.id));
      await this.load();
    } catch (e) {
      this.error.set(extractError(e));
    }
  }

  private async load(): Promise<void> {
    this.error.set(null);
    this.categories.set(await firstValueFrom(this.api.adminGetCategories()));
  }
}
