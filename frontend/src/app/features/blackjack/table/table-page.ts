import { Component, computed, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Seat, TableView } from '../../../core/models/blackjack.models';
import { SessionStore } from '../../../core/session/session-store';
import { formatDrinks } from '../../../shared/pipes/drinks-pipe';
import { DealerMood } from '../../../shared/ui/dealer-3d/dealer-3d';
import { DealSequencer } from '../../../shared/ui/dealing/deal-sequencer';
import { seatPositions } from '../../table-layout/table-geometry';
import { TableLayout } from '../../table-layout/table-layout';
import { DealerHand } from '../components/dealer-hand/dealer-hand';
import { TableStore } from '../state/table-store';
import { SeatView } from './seat/seat';

function describePhase(view: TableView, hasTimer: boolean): string {
  switch (view.phase) {
    case 'Betting':
      if (view.seats.length === 0) {
        return 'En attente de joueurs';
      }
      return hasTimer ? 'Faites vos jeux' : 'En attente des mises';
    case 'PlayerTurns': {
      const active = view.seats.find((seat) => seat.playerId === view.activePlayerId);
      return active ? `Au tour de ${active.name}` : 'Tour des joueurs';
    }
    case 'DealerTurn':
      return 'Le dealer joue';
    case 'Distribution':
      return 'Les gagnants distribuent';
    case 'Results':
      return 'Bilan de la manche';
  }
}

/**
 * Page table du blackjack (admin) : écran partagé qui montre toutes les places et toutes
 * les cartes. La mise en page vient de TableLayout ; ici, seulement ce qui est propre au jeu.
 */
@Component({
  selector: 'bj-table-page',
  imports: [DealerHand, RouterLink, SeatView, TableLayout],
  providers: [TableStore, DealSequencer],
  templateUrl: './table-page.html',
  styleUrl: './table-page.scss',
})
export class TablePage {
  protected readonly store = inject(TableStore);
  protected readonly view = this.store.view;

  protected readonly phaseText = computed(() => {
    const view = this.view();
    return view ? describePhase(view, this.store.deadline() !== null) : '';
  });

  /** Les places de la table, occupées ou libres, avec leur position sur l'arc. */
  protected readonly seatSlots = computed(() => {
    const view = this.view();
    if (!view) {
      return [];
    }
    return seatPositions(view.maxPlayers).map((position, index) => ({
      seat: view.seats[index] ?? null,
      position,
      index,
    }));
  });

  /** Le dealer fait la tête quand il saute, et jubile sur un blackjack. */
  protected readonly dealerMood = computed<DealerMood>(() => {
    const dealer = this.view()?.dealer;
    if (dealer?.isBust) {
      return 'sad';
    }
    return dealer?.isBlackjack ? 'happy' : 'idle';
  });

  /** « 2 à 5 gorgées, ou cul sec ». */
  protected readonly limitsText = computed(() => {
    const limits = this.view()?.betLimits;
    if (!limits) {
      return '';
    }
    const range =
      limits.min === limits.max
        ? formatDrinks(limits.max)
        : `${limits.min} à ${formatDrinks(limits.max)}`;
    return limits.allowCulSec ? `${range}, ou cul sec` : range;
  });

  protected readonly shoePercent = computed(() => {
    const shoe = this.view()?.shoe;
    return shoe && shoe.total > 0 ? Math.round((shoe.remaining / shoe.total) * 100) : 0;
  });

  constructor() {
    // Le garde de route garantit la session admin de cette table.
    const session = inject(SessionStore).session();
    if (session?.role === 'admin') {
      this.store.connect(session);
    }

    // Les cartes déjà sur la table à l'arrivée ne sont pas redistribuées : on n'anime
    // qu'à partir du moment où le premier état est affiché.
    const sequencer = inject(DealSequencer);
    effect(() => {
      if (this.view()) {
        setTimeout(() => sequencer.arm());
      }
    });
  }

  protected kick(seat: Seat): void {
    if (confirm(`Expulser ${seat.name} de la table ?`)) {
      this.store.kick(seat.playerId);
    }
  }

  protected closeTable(): void {
    if (confirm('Fermer la table ? La partie sera terminée pour tout le monde.')) {
      this.store.closeTable();
    }
  }
}
