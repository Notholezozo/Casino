import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionStore } from './session-store';
import { Session } from './session.model';

function hasSessionFor(session: Session | null, role: Session['role'], code: string): boolean {
  return session?.role === role && session.code.toUpperCase() === code.toUpperCase();
}

/** /play/:code — exige une session joueur pour ce code, sinon accueil avec le code prérempli. */
export const playerSessionGuard: CanActivateFn = (route) => {
  const code = route.paramMap.get('code') ?? '';
  if (hasSessionFor(inject(SessionStore).session(), 'player', code)) {
    return true;
  }
  return inject(Router).createUrlTree(['/'], { queryParams: { code } });
};

/** /table/:code — exige la session admin de cette table. */
export const adminSessionGuard: CanActivateFn = (route) => {
  const code = route.paramMap.get('code') ?? '';
  if (hasSessionFor(inject(SessionStore).session(), 'admin', code)) {
    return true;
  }
  return inject(Router).createUrlTree(['/']);
};
