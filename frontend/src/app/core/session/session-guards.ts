import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { GameKind } from '../models/table.models';
import { SessionStore } from './session-store';
import { Session } from './session.model';

function hasSessionFor(session: Session | null, role: Session['role'], code: string): boolean {
  return session?.role === role && session.code.toUpperCase() === code.toUpperCase();
}

/**
 * Aiguille /play et /table vers l'interface du jeu de la table : la route ne correspond que
 * si la session en cours est celle d'une table de ce jeu.
 */
export function sessionGameIs(game: GameKind): CanMatchFn {
  return () => inject(SessionStore).session()?.game === game;
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
