import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth-store';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  return auth.isLoggedIn() ? true : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  if (auth.isAdmin()) return true;
  return auth.isLoggedIn() ? router.createUrlTree(['/']) : router.createUrlTree(['/login'], { queryParams: { returnUrl: '/admin' } });
};

export const deliveryGuard: CanActivateFn = () => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  if (auth.isCourier()) return true;
  return auth.isLoggedIn() ? router.createUrlTree(['/']) : router.createUrlTree(['/login'], { queryParams: { returnUrl: '/delivery' } });
};
