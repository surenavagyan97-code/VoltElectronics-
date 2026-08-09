import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '../core/auth-store';
import { CartStore, extractError } from '../core/cart-store';
import { I18nService } from '../core/i18n.service';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  template: `
    <div style="padding: 64px 32px; max-width: 400px; margin: 0 auto;">
      <h2 style="margin-bottom: 6px;">{{ i18n.t('auth.createAccountTitle') }}</h2>
      <p class="text-muted" style="margin-bottom: 24px;">{{ i18n.t('auth.trackOrders') }}</p>
      <form class="col" style="gap: 14px;" (ngSubmit)="submit()">
        <div class="field"><label>{{ i18n.t('common.fullName') }}</label>
          <input class="input" name="fullName" [(ngModel)]="fullName" required autocomplete="name" />
        </div>
        <div class="field"><label>{{ i18n.t('common.email') }}</label>
          <input class="input" type="email" name="email" [(ngModel)]="email" required autocomplete="email" />
        </div>
        <div class="field"><label>{{ i18n.t('auth.password') }} <span class="text-muted">{{ i18n.t('auth.passwordHint') }}</span></label>
          <input class="input" type="password" name="password" [(ngModel)]="password" required minlength="8" autocomplete="new-password" />
        </div>
        @if (error(); as err) { <div class="error-text">{{ err }}</div> }
        <button class="btn btn-primary btn-block" type="submit" [disabled]="busy()">
          {{ busy() ? i18n.t('auth.creatingAccount') : i18n.t('auth.createAccountTitle') }}
        </button>
      </form>
      <div class="hr"></div>
      <p class="text-muted" style="font-size: 14px;">{{ i18n.t('auth.alreadyHaveAccount') }} <a routerLink="/login">{{ i18n.t('auth.signIn') }}</a></p>
    </div>
  `,
})
export class RegisterPage {
  private auth = inject(AuthStore);
  private cart = inject(CartStore);
  private router = inject(Router);
  i18n = inject(I18nService);

  fullName = '';
  email = '';
  password = '';
  busy = signal(false);
  error = signal<string | null>(null);

  async submit(): Promise<void> {
    if (!this.fullName || !this.email || !this.password) return;
    this.busy.set(true);
    this.error.set(null);
    try {
      await this.auth.register(this.email, this.password, this.fullName);
      await this.cart.mergeAfterLogin();
      void this.router.navigateByUrl('/');
    } catch (e) {
      this.error.set(extractError(e));
    } finally {
      this.busy.set(false);
    }
  }
}
