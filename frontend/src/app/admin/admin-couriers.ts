import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { Courier } from '../core/api.types';
import { extractError } from '../core/cart-store';
import { I18nService } from '../core/i18n.service';

@Component({
  selector: 'app-admin-couriers',
  imports: [FormsModule],
  styles: `.table-scroll { overflow-x: auto; }`,
  template: `
    <div class="row" style="justify-content: space-between; margin-bottom: 20px; gap: 12px; flex-wrap: wrap;">
      <h2 style="margin: 0;">{{ i18n.t('admin.couriers.title') }}</h2>
      <button class="btn btn-primary" (click)="startCreate()">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
        {{ i18n.t('admin.couriers.addCourier') }}
      </button>
    </div>

    <p class="text-muted" style="margin-bottom: 16px; font-size: 13px;">{{ i18n.t('admin.couriers.hint') }}</p>

    @if (error(); as err) { <div class="error-text" style="margin-bottom: 10px;">{{ err }}</div> }

    @if (creating()) {
      <div class="card elev-sm" style="margin-bottom: 20px;">
        <div class="card-kicker" style="margin-bottom: 10px;">{{ i18n.t('admin.couriers.newCourier') }}</div>
        <form class="row" style="gap: 10px; flex-wrap: wrap; align-items: flex-end;" (ngSubmit)="saveCreate()">
          <div class="field" style="margin: 0; width: 200px;"><label>{{ i18n.t('admin.couriers.fullName') }}</label>
            <input class="input" name="fullName" [(ngModel)]="newFullName" required />
          </div>
          <div class="field" style="margin: 0; width: 220px;"><label>{{ i18n.t('common.email') }}</label>
            <input class="input" type="email" name="email" [(ngModel)]="newEmail" required autocomplete="off" />
          </div>
          <div class="field" style="margin: 0; width: 200px;"><label>{{ i18n.t('admin.couriers.password') }}</label>
            <input class="input" type="text" name="password" [(ngModel)]="newPassword" required autocomplete="new-password" [placeholder]="i18n.t('admin.couriers.passwordHint')" />
          </div>
          <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ i18n.t('common.save') }}</button>
          <button class="btn btn-secondary" type="button" (click)="creating.set(false)">{{ i18n.t('common.cancel') }}</button>
        </form>
      </div>
    }

    <div class="table-scroll">
    <table class="table">
      <thead><tr><th>{{ i18n.t('admin.couriers.table.name') }}</th><th>{{ i18n.t('common.email') }}</th><th>{{ i18n.t('admin.couriers.table.activeOrders') }}</th><th></th></tr></thead>
      <tbody>
        @for (c of couriers(); track c.id) {
          <tr>
            <td>{{ c.fullName }}</td>
            <td class="text-muted">{{ c.email }}</td>
            <td>{{ c.activeOrderCount }}</td>
            <td>
              <div class="row" style="gap: 4px; justify-content: flex-end;">
                <button class="btn btn-icon btn-ghost" (click)="remove(c)" [attr.aria-label]="i18n.t('common.delete')">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M3 6h18"></path><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"></path></svg>
                </button>
              </div>
            </td>
          </tr>
        } @empty {
          <tr><td colspan="4" class="text-muted" style="text-align: center; padding: 24px;">{{ i18n.t('admin.couriers.empty') }}</td></tr>
        }
      </tbody>
    </table>
    </div>
  `,
})
export class AdminCouriersPage implements OnInit {
  private api = inject(ApiClient);
  i18n = inject(I18nService);

  couriers = signal<Courier[]>([]);
  error = signal<string | null>(null);
  creating = signal(false);
  busy = signal(false);
  newFullName = '';
  newEmail = '';
  newPassword = '';

  async ngOnInit(): Promise<void> { await this.load(); }

  startCreate(): void {
    this.newFullName = '';
    this.newEmail = '';
    this.newPassword = '';
    this.creating.set(true);
  }

  async saveCreate(): Promise<void> {
    if (!this.newFullName.trim() || !this.newEmail.trim() || !this.newPassword) return;
    this.busy.set(true);
    try {
      await firstValueFrom(this.api.adminCreateCourier(this.newEmail.trim(), this.newPassword, this.newFullName.trim()));
      this.creating.set(false);
      this.error.set(null);
      await this.load();
    } catch (e) {
      this.error.set(extractError(e));
    } finally {
      this.busy.set(false);
    }
  }

  async remove(courier: Courier): Promise<void> {
    if (!confirm(this.i18n.t('admin.couriers.deleteConfirm', { name: courier.fullName }))) return;
    try {
      await firstValueFrom(this.api.adminDeleteCourier(courier.id));
      await this.load();
    } catch (e) {
      this.error.set(extractError(e));
    }
  }

  private async load(): Promise<void> {
    this.couriers.set(await firstValueFrom(this.api.adminGetCouriers()));
  }
}
