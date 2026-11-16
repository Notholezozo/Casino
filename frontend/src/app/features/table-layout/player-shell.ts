import { Component, computed, input, output } from '@angular/core';
import { DrinkTotals } from '../../core/models/blackjack.models';
import { formatTally } from '../../shared/pipes/drinks-pipe';
import { RoomCode } from '../../shared/ui/room-code/room-code';

/**
 * Mise en page de la vue joueur (téléphone), commune à tous les jeux : l'en-tête avec le
 * pseudo, le code et les cumuls, le tapis au milieu, et les actions en bas sous le pouce.
 *
 * Chaque jeu y projette son tapis (contenu par défaut) et ses actions (`playerControls`).
 */
@Component({
  selector: 'app-player-shell',
  imports: [RoomCode],
  template: `
    <header class="topbar">
      <div class="who">
        <h1 class="who__name">{{ name() }}</h1>
        <app-room-code [code]="tableCode()" size="sm" />
      </div>
      <div class="topbar__side">
        @if (reconnecting()) {
          <span class="badge badge--burgundy">Reconnexion…</span>
        }
        <p class="tally">Bu {{ tally().drunk }} · Donné {{ tally().given }}</p>
      </div>
      <button type="button" class="btn btn--ghost" (click)="leave.emit()">Quitter</button>
    </header>

    <main class="table">
      <ng-content />
    </main>

    <footer class="controls">
      <ng-content select="[playerControls]" />
    </footer>
  `,
  styleUrl: './player-shell.scss',
})
export class PlayerShell {
  readonly name = input.required<string>();
  readonly tableCode = input.required<string>();
  /** Cumuls du joueur depuis son arrivée à la table. */
  readonly totals = input.required<DrinkTotals>();
  readonly reconnecting = input(false);

  readonly leave = output<void>();

  protected readonly tally = computed(() => {
    const totals = this.totals();
    return {
      drunk: formatTally(totals.sipsDrunk, totals.culsSecsDrunk),
      given: formatTally(totals.sipsGiven, totals.culsSecsGiven),
    };
  });
}
