/** Corps d'erreur renvoyé par l'API (RFC 7807 + code métier). */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
}
