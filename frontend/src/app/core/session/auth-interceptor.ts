import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { SessionExit } from './session-exit';
import { SessionStore } from './session-store';

const SESSION_EXPIRED = 'Ta session a expiré : la table a été fermée ou le serveur a redémarré.';

/**
 * Ajoute le jeton de session aux appels API et gère son expiration :
 * un 401 sur un appel authentifié vide la session et renvoie à l'accueil.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const sessions = inject(SessionStore);
  const exit = inject(SessionExit);

  const token = sessions.session()?.token;
  if (!token || !request.url.startsWith('/api/')) {
    return next(request);
  }

  const authorized = request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  return next(authorized).pipe(
    catchError((error: unknown) => {
      const isCurrentSession = sessions.session()?.token === token;
      if (error instanceof HttpErrorResponse && error.status === 401 && isCurrentSession) {
        exit.toHome(SESSION_EXPIRED);
      }
      return throwError(() => error);
    }),
  );
};
