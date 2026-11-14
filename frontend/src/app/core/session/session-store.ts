import { Injectable, signal } from '@angular/core';
import { Session, isSession } from './session.model';

const STORAGE_KEY = 'casino.session';

/**
 * Session invité courante (une seule à la fois).
 *
 * Écrite dans localStorage pour survivre à la fermeture du navigateur, et dans
 * sessionStorage pour que chaque onglet garde la sienne après un rechargement
 * (pratique pour avoir la page table et un joueur sur le même PC).
 */
@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly current = signal<Session | null>(this.restore());

  readonly session = this.current.asReadonly();

  set(session: Session): void {
    this.current.set(session);
    const serialized = JSON.stringify(session);
    this.write(sessionStorage, serialized);
    this.write(localStorage, serialized);
  }

  clear(): void {
    const token = this.current()?.token;
    this.current.set(null);
    this.remove(sessionStorage);
    // Ne pas effacer la session d'un autre onglet qui aurait pris le relais.
    const persisted = this.read(localStorage);
    if (!persisted || persisted.token === token) {
      this.remove(localStorage);
    }
  }

  private restore(): Session | null {
    return this.read(sessionStorage) ?? this.read(localStorage);
  }

  private read(storage: Storage): Session | null {
    try {
      const raw = storage.getItem(STORAGE_KEY);
      const parsed: unknown = raw ? JSON.parse(raw) : null;
      return isSession(parsed) ? parsed : null;
    } catch {
      return null;
    }
  }

  private write(storage: Storage, value: string): void {
    try {
      storage.setItem(STORAGE_KEY, value);
    } catch {
      // Stockage indisponible (navigation privée) : la session vit en mémoire.
    }
  }

  private remove(storage: Storage): void {
    try {
      storage.removeItem(STORAGE_KEY);
    } catch {
      // Rien à nettoyer.
    }
  }
}
