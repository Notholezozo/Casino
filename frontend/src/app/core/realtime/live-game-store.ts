import { DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, finalize } from 'rxjs';
import { describeApiError } from '../api/api-error';
import { LiveState } from '../models/table.models';
import { GameHub } from './game-hub';
import { SessionExit } from '../session/session-exit';

const ERROR_DISPLAY_MS = 4000;
/** En dessous de cet écart, deux échéances successives sont le même minuteur (gigue réseau). */
const DEADLINE_JITTER_MS = 1000;

/**
 * Socle des stores de page (joueur / table) : garde la dernière vue poussée par le
 * serveur, convertit `remainingMs` en échéance locale et exécute les commandes.
 * À fournir au niveau du composant de page : sa durée de vie est celle de la page.
 */
export abstract class LiveGameStore<TView extends LiveState> {
  protected readonly hub = inject(GameHub);
  protected readonly exit = inject(SessionExit);
  protected readonly destroyRef = inject(DestroyRef);

  private readonly currentView = signal<TView | null>(null);
  private readonly currentDeadline = signal<number | null>(null);
  private readonly currentError = signal<string | null>(null);
  private readonly pendingCommands = signal(0);
  private errorTimer: ReturnType<typeof setTimeout> | undefined;

  readonly view = this.currentView.asReadonly();
  /** Échéance de la phase en epoch ms locales, ou null sans minuteur. */
  readonly deadline = this.currentDeadline.asReadonly();
  readonly error = this.currentError.asReadonly();
  /** Vrai tant qu'une commande attend sa réponse. */
  readonly busy = computed(() => this.pendingCommands() > 0);
  readonly hubStatus = this.hub.status;

  /** Charge l'état courant par HTTP (valide aussi la session : un 401 renvoie à l'accueil). */
  protected abstract fetchState(): Observable<TView>;

  /** Ouvre la connexion temps réel ; `event` est le flux d'états propre au rôle. */
  protected open(token: string, event: 'PlayerState' | 'TableState'): void {
    // Le hub ne connaît pas la forme des vues : chaque store sait laquelle son jeu lui envoie.
    (this.hub.on(event) as Observable<TView>).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((view) => this.apply(view));

    this.hub
      .on('TableClosed')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.exit.toHome('La table a été fermée.'));

    // Si le hub n'arrive pas à démarrer, un appel HTTP dit si la session est encore valide.
    this.hub.startFailed$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.refresh());

    this.destroyRef.onDestroy(() => {
      clearTimeout(this.errorTimer);
      this.hub.disconnect();
    });

    this.refresh();
    this.hub.connect(token);
  }

  protected refresh(): void {
    this.fetchState()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (view) => this.apply(view),
        error: () => undefined, // 401 géré par l'intercepteur ; sinon le hub réessaie.
      });
  }

  /** Exécute une commande ; son effet revient par le hub, seule l'erreur est gérée ici. */
  protected run(command: Observable<unknown>, onSuccess?: () => void): void {
    this.pendingCommands.update((count) => count + 1);
    command
      .pipe(
        finalize(() => this.pendingCommands.update((count) => count - 1)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => onSuccess?.(),
        error: (error: unknown) => this.showError(describeApiError(error)),
      });
  }

  private apply(view: TView): void {
    const previous = this.currentView();
    if (previous && view.version < previous.version) {
      return;
    }
    this.currentView.set(view);
    this.currentDeadline.set(this.resolveDeadline(view, previous));
  }

  private resolveDeadline(view: TView, previous: TView | null): number | null {
    if (view.remainingMs === null) {
      return null;
    }
    const deadline = Date.now() + view.remainingMs;
    const current = this.currentDeadline();
    const sameTimer =
      current !== null &&
      previous?.phase === view.phase &&
      Math.abs(deadline - current) < DEADLINE_JITTER_MS;
    return sameTimer ? current : deadline;
  }

  private showError(message: string): void {
    this.currentError.set(message);
    clearTimeout(this.errorTimer);
    this.errorTimer = setTimeout(() => this.currentError.set(null), ERROR_DISPLAY_MS);
  }
}
