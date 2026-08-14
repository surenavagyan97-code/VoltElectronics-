import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from './api-client';
import { AuthResponse, AuthUser } from './api.types';

const STORAGE_KEY = 'volt.auth';

interface StoredAuth {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
}

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private api = inject(ApiClient);

  private auth = signal<StoredAuth | null>(readStored());

  readonly user = computed(() => this.auth()?.user ?? null);
  readonly isLoggedIn = computed(() => this.auth() !== null);
  readonly isAdmin = computed(() => this.auth()?.user.roles.includes('Admin') ?? false);
  readonly isCourier = computed(() => this.auth()?.user.roles.includes('Courier') ?? false);

  get accessToken(): string | null { return this.auth()?.accessToken ?? null; }

  async login(email: string, password: string): Promise<void> {
    this.setAuth(await firstValueFrom(this.api.login(email, password)));
  }

  async register(email: string, password: string, fullName: string): Promise<void> {
    this.setAuth(await firstValueFrom(this.api.register(email, password, fullName)));
  }

  /** Rotate the refresh token; returns the new access token or null (session expired). */
  async tryRefresh(): Promise<string | null> {
    const current = this.auth();
    if (!current) return null;
    try {
      const res = await firstValueFrom(this.api.refresh(current.refreshToken));
      this.setAuth(res);
      return res.accessToken;
    } catch {
      this.clear();
      return null;
    }
  }

  async logout(): Promise<void> {
    const current = this.auth();
    if (current) {
      try { await firstValueFrom(this.api.logout(current.refreshToken)); } catch { /* best effort */ }
    }
    this.clear();
  }

  private setAuth(res: AuthResponse): void {
    const stored: StoredAuth = { accessToken: res.accessToken, refreshToken: res.refreshToken, user: res.user };
    this.auth.set(stored);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
  }

  private clear(): void {
    this.auth.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }
}

function readStored(): StoredAuth | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as StoredAuth) : null;
  } catch {
    return null;
  }
}
