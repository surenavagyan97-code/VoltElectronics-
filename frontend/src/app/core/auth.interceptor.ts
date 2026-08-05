import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthStore } from './auth-store';
import { CartStore } from './cart-store';

/** Attaches the JWT and the guest X-Cart-Id; on 401 tries one token refresh and retries. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthStore);

  const prepared = decorate(req, auth.accessToken);
  const isAuthCall = req.url.startsWith('/api/auth/');

  return next(prepared).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 401 || isAuthCall || !auth.isLoggedIn()) {
        return throwError(() => err);
      }
      return from(auth.tryRefresh()).pipe(
        switchMap((token) =>
          token ? next(decorate(req, token)) : throwError(() => err)),
      );
    }),
  );
};

function decorate(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  const headers: Record<string, string> = { 'X-Cart-Id': CartStore.guestId() };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  return req.clone({ setHeaders: headers });
}
