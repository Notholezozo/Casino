/** Session de la page table : celui qui a créé la table. */
export interface AdminSession {
  role: 'admin';
  code: string;
  token: string;
}

/** Session invité d'un joueur assis à une table. */
export interface PlayerSession {
  role: 'player';
  code: string;
  token: string;
  playerId: string;
  name: string;
}

export type Session = AdminSession | PlayerSession;

export function isSession(value: unknown): value is Session {
  if (!value || typeof value !== 'object') {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  const hasBase = typeof candidate['code'] === 'string' && typeof candidate['token'] === 'string';
  if (!hasBase) {
    return false;
  }
  if (candidate['role'] === 'admin') {
    return true;
  }
  return (
    candidate['role'] === 'player' &&
    typeof candidate['playerId'] === 'string' &&
    typeof candidate['name'] === 'string'
  );
}
