import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Seat, TableView } from '../../../core/models/blackjack.models';
import { SessionStore } from '../../../core/session/session-store';
import { SipsPipe } from '../../../shared/pipes/sips-pipe';
import { Countdown } from '../../../shared/ui/countdown/countdown';
import { QrCode } from '../../../shared/ui/qr-code/qr-code';
import { DealerHand } from '../components/dealer-hand/dealer-hand';
import { TableStore } from '../state/table-store';
import { SeatView } from './seat/seat';

function describePhase(view: TableView, hasTimer: boolean): string {
  switch (view.phase) {
    case 'Betting':
      if (view.seats.length === 0) {
        return 'En attente de joueurs';
      }
      return hasTimer ? 'Faites vos jeux !' : 'En attente des mises';
    case 'PlayerTurns': {
      const active = view.seats.find((seat) => seat.playerId === view.activePlayerId);
      return active ? `Au tour de ${active.name}` : 'Tour des joueurs';
    }
    case 'DealerTurn':
      return 'Le croupier joue';
    case 'Distribution':
      return 'Distribution des gorgées';
    case 'Results':
      return 'Bilan de la manche';
  }
}

/** Page table (admin) : écran partagé qui montre tous les sièges et toutes les cartes. */
@Component({
  selector: 'bj-table-page',
  imports: [Countdown, DealerHand, QrCode, RouterLink, SeatView, SipsPipe],
  providers: [TableStore],
  templateUrl: './table-page.html',
  styleUrl: './table-page.scss',
})
export class TablePage {
  protected readonly store = inject(TableStore);
  protected readonly view = this.store.view;

  /** Lien d'invitation : l'accueil avec le code prérempli. */
  protected readonly inviteUrl: string;
  /** Sur localhost, le QR code ne mène nulle part depuis un téléphone. */
  protected readonly isLocalOnly = ['localhost', '127.0.0.1', '[::1]'].includes(location.hostname);

  protected readonly phaseText = computed(() => {
    const view = this.view();
    return view ? describePhase(view, this.store.deadline() !== null) : '';
  });

  /** Sièges occupés complétés par des sièges libres jusqu'à la capacité de la table. */
  protected readonly seatSlots = computed<(Seat | null)[]>(() => {
    const view = this.view();
    if (!view) {
      return [];
    }
    const free = Math.max(view.maxPlayers - view.seats.length, 0);
    return [...view.seats, ...Array.from({ length: free }, () => null)];
  });

  /** Qui boit combien cette manche, du plus au moins assoiffé. */
  protected readonly drinkers = computed(() => {
    const view = this.view();
    if (!view || (view.phase !== 'Distribution' && view.phase !== 'Results')) {
      return [];
    }
    return view.seats
      .filter((seat) => seat.result !== null)
      .map((seat) => ({ name: seat.name, sips: seat.result!.sipsToDrink }))
      .sort((a, b) => b.sips - a.sips);
  });

  constructor() {
    // Le garde de route garantit la session admin de cette table.
    const session = inject(SessionStore).session();
    this.inviteUrl = `${location.origin}/?code=${encodeURIComponent(session?.code ?? '')}`;
    if (session?.role === 'admin') {
      this.store.connect(session);
    }
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
