import { Injectable, computed, signal } from '@angular/core';

const STORAGE_KEY = 'sb_compare';

/** Side-by-side comparison only makes sense for a handful of products at once. */
export const COMPARE_MAX = 10;

function read(): number[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : [];
    return Array.isArray(parsed) ? parsed.filter((x): x is number => typeof x === 'number') : [];
  } catch {
    return [];
  }
}

/** Compare-list product ids, kept in the browser so guests get this too. */
@Injectable({ providedIn: 'root' })
export class CompareStore {
  readonly ids = signal<number[]>(read());
  readonly count = computed(() => this.ids().length);
  readonly isFull = computed(() => this.ids().length >= COMPARE_MAX);

  has(id: number): boolean {
    return this.ids().includes(id);
  }

  /** No-op (returns false) when adding would exceed COMPARE_MAX; removing always succeeds. */
  toggle(id: number): boolean {
    if (!this.ids().includes(id) && this.isFull()) return false;
    this.ids.update((ids) => (ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]));
    localStorage.setItem(STORAGE_KEY, JSON.stringify(this.ids()));
    return true;
  }

  remove(id: number): void {
    this.ids.update((ids) => ids.filter((x) => x !== id));
    localStorage.setItem(STORAGE_KEY, JSON.stringify(this.ids()));
  }

  clear(): void {
    this.ids.set([]);
    localStorage.removeItem(STORAGE_KEY);
  }
}
