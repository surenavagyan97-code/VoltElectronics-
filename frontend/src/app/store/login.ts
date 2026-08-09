import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthStore } from '../core/auth-store';
import { CartStore, extractError } from '../core/cart-store';
import { I18nService } from '../core/i18n.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  template: `
    <div style="padding: 64px 32px; max-width: 400px; margin: 0 auto;">
      <h2 style="margin-bottom: 6px;">{{ i18n.t('auth.signIn') }}</h2>
      <p class="text-muted" style="margin-bottom: 24px;">{{ i18n.t('auth.welcomeBack') }}</p>
      <form class="col" style="gap: 14px;" (ngSubmit)="submit()">
        <div class="field"><label>{{ i18n.t('common.email') }}</label>
          <input class="input" type="email" name="email" [(ngModel)]="email" required autocomplete="email" />
        </div>
        <div class="field"><label>{{ i18n.t('auth.password') }}</label>
          <input class="input" type="password" name="password" [(ngModel)]="password" required autocomplete="current-password" />
        </div>
        @if (error(); as err) { <div class="error-text">{{ err }}</div> }
        <button class="btn btn-primary btn-block" type="submit" [disabled]="busy()">
          {{ busy() ? i18n.t('auth.signingIn') : i18n.t('auth.signIn') }}
        </button>
      </form>
      <div class="hr"></div>
      <p class="text-muted" style="font-size: 14px;">{{ i18n.t('auth.newHere') }} <a routerLink="/register">{{ i18n.t('auth.createAccountLink') }}</a></p>
    </div>
  `,
})
export class LoginPage {
  private auth = inject(AuthStore);
  private cart = inject(CartStore);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  i18n = inject(I18nService);

  email = '';
  password = '';
  busy = signal(false);
  error = signal<string | null>(null);

  async submit(): Promise<void> {
    if (!this.email || !this.password) return;
    this.busy.set(true);
    this.error.set(null);
    try {
      await this.auth.login(this.email, this.password);
      await this.cart.mergeAfterLogin();
      void this.router.navigateByUrl(this.route.snapshot.queryParamMap.get('returnUrl') ?? '/');
    } catch (e) {
      this.error.set(extractError(e));
    } finally {
      this.busy.set(false);
    }
  }
}
