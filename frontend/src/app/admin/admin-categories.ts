import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { Category } from '../core/api.types';
import { extractError } from '../core/cart-store';

@Component({
  selector: 'app-admin-categories',
  imports: [FormsModule],
  template: `
    <div class="row" style="justify-content: space-between; margin-bottom: 20px;">
      <h2 style="margin: 0;">Categories</h2>
      <button class="btn btn-primary" (click)="startCreate()">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
        Add category
      </button>
    </div>

    @if (error(); as err) { <div class="error-text" style="margin-bottom: 10px;">{{ err }}</div> }

    <table class="table">
      <thead><tr><th>Category</th><th>Slug</th><th>Products</th><th></th></tr></thead>
      <tbody>
        @for (cat of categories(); track cat.id) {
          <tr>
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
                  <button class="btn btn-ghost" (click)="saveEdit(cat)">Save</button>
                  <button class="btn btn-secondary" (click)="editingId.set(null)">Cancel</button>
                } @else {
                  <button class="btn btn-icon btn-ghost" (click)="startEdit(cat)" aria-label="Edit">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M12 20h9"></path><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z"></path></svg>
                  </button>
                  <button class="btn btn-icon btn-ghost" (click)="remove(cat)" aria-label="Delete">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M3 6h18"></path><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"></path></svg>
                  </button>
                }
              </div>
            </td>
          </tr>
        }
        @if (creating()) {
          <tr>
            <td><input class="input" style="width: 220px;" placeholder="Category name" [(ngModel)]="newName" (keyup.enter)="saveCreate()" /></td>
            <td class="text-muted">—</td>
            <td>0</td>
            <td>
              <div class="row" style="gap: 4px; justify-content: flex-end;">
                <button class="btn btn-ghost" (click)="saveCreate()">Save</button>
                <button class="btn btn-secondary" (click)="creating.set(false)">Cancel</button>
              </div>
            </td>
          </tr>
        }
      </tbody>
    </table>
  `,
})
export class AdminCategoriesPage implements OnInit {
  private api = inject(ApiClient);

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
