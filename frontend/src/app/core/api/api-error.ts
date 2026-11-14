import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/problem-details';

const UNREACHABLE = 'Serveur injoignable. Vérifie ta connexion.';
const UNKNOWN = 'Une erreur est survenue. Réessaie.';

/** Traduit une erreur HTTP en message affichable (le `detail` de l'API est déjà en français). */
export function describeApiError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return UNKNOWN;
  }
  if (error.status === 0) {
    return UNREACHABLE;
  }
  const problem = error.error as ProblemDetails | null;
  if (problem && typeof problem === 'object') {
    return problem.detail || problem.title || UNKNOWN;
  }
  return UNKNOWN;
}
