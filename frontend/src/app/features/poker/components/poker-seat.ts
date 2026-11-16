import { Component, computed, input, output } from '@angular/core';
import { PokerPhase, PokerSeat } from '../../../core/models/poker.models';
import { DrinksPipe } from '../../../shared/pipes/drinks-pipe';
import { PlayingCard } from '../../../shared/ui/playing-card/playing-card';
import { isBettingPhase, seatRole } from '../poker-labels';

/**
 * Une place de la table de poker vue sur l'écran partagé : joueur, rôle, cartes (face cachée
 * jusqu'à l'abattage), mise et résultat. Sans `seat`, place libre.
 */
@Component({
  selector: 'pk-seat',
  imports: [DrinksPipe, PlayingCard],
  templateUrl: './poker-seat.html',
  styleUrl: './poker-seat.scss',
  host: {
    '[class.is-empty]': '!seat()',
    '[class.is-active]': 'seat()?.isActive',
    '[class.is-offline]': 'seat() && !seat()!.isConnected',
    '[class.is-folded]': 'seat()?.folded',
    '[class.is-winner]': 'seat()?.isWinner',
  },
})
export class PokerSeatView {
  readonly seat = input<PokerSeat | null>(null);
  readonly phase = input.required<PokerPhase>();
  /** Position autour de la table, pour l'ordre de distribution des cartes. */
  readonly dealRank = input(0);

  readonly kick = output<void>();

  protected readonly role = computed(() => {
    const seat = this.seat();
    return seat ? seatRole(seat) : null;
  });

  /** Deux emplacements : les cartes si on peut les voir, sinon deux dos. */
  protected readonly cardSlots = computed(() => {
    const seat = this.seat();
    if (!seat?.hasCards) {
      return [];
    }
    return seat.cards ?? [null, null];
  });

  /** Ce que le joueur a misé sur le tour d'enchères en cours, tant qu'on mise. */
  protected readonly showBet = computed(() => isBettingPhase(this.phase()) && !!this.seat()?.inHand);

  /** Le bilan n'est affiché qu'une fois la main jouée. */
  protected readonly showResult = computed(() => {
    const phase = this.phase();
    return phase === 'Distribution' || phase === 'Results';
  });
}
