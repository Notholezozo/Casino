import { Component, computed, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PokerSeat } from '../../../core/models/poker.models';
import { SessionStore } from '../../../core/session/session-store';
import { DealerMood } from '../../../shared/ui/dealer-3d/dealer-3d';
import { DealSequencer } from '../../../shared/ui/dealing/deal-sequencer';
import { seatPositions } from '../../table-layout/table-geometry';
import { TableLayout } from '../../table-layout/table-layout';
import { Board } from '../components/board';
import { PokerSeatView } from '../components/poker-seat';
import { describePokerPhase } from '../poker-labels';
import { PokerTableStore } from '../state/poker-stores';

/**
 * Page table du poker (admin) : écran partagé, vu de tous. Les cartes privées n'y
 * apparaissent qu'à l'abattage. La mise en page vient de TableLayout.
 */
@Component({
  selector: 'pk-table-page',
  imports: [Board, PokerSeatView, RouterLink, TableLayout],
  providers: [PokerTableStore, DealSequencer],
  templateUrl: './poker-table-page.html',
  styleUrl: './poker-table-page.scss',
})
export class PokerTablePage {
  protected readonly store = inject(PokerTableStore);
  protected readonly view = this.store.view;

  protected readonly phaseText = computed(() => {
    const view = this.view();
    return view ? describePokerPhase(view) : '';
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

  /** Le croupier s'anime à l'abattage. */
  protected readonly dealerMood = computed<DealerMood>(() => {
    const view = this.view();
    return view?.showdown && view.phase !== 'Waiting' ? 'happy' : 'idle';
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

  protected kick(seat: PokerSeat): void {
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
