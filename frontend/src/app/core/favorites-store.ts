import { Injectable, computed, signal } from '@angular/core';

const STORAGE_KEY = 'sb_favorites';

function read(): number[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : [];
    return Array.isArray(parsed) ? parsed.filter((x): x is number => typeof x === 'number') : [];
  } catch {
    return [];
  }
}

/** Favorite product ids, kept in the browser so guests get a wishlist too. */
@Injectable({ providedIn: 'root' })
export class FavoritesStore {
  readonly ids = signal<number[]>(read());
  readonly count = computed(() => this.ids().length);

  has(id: number): boolean {
    return this.ids().includes(id);
  }

  toggle(id: number): void {
    this.ids.update((ids) => (ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]));
    localStorage.setItem(STORAGE_KEY, JSON.stringify(this.ids()));
  }
}
