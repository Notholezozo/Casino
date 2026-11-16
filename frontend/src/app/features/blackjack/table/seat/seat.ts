import { Component, computed, input, output } from '@angular/core';
import { GamePhase, Seat } from '../../../../core/models/blackjack.models';
import { DrinksPipe, formatToDrink } from '../../../../shared/pipes/drinks-pipe';
import { HandView } from '../../components/hand/hand';

/** Une place de la table vue par l'admin : joueur, mains, mise et bilan. Sans `seat`, place libre. */
@Component({
  selector: 'bj-seat',
  imports: [DrinksPipe, HandView],
  templateUrl: './seat.html',
  styleUrl: './seat.scss',
  host: {
    '[class.is-empty]': '!seat()',
    '[class.is-active]': 'seat()?.isActive',
    '[class.is-offline]': 'seat() && !seat()!.isConnected',
    '[class.has-split]': '(seat()?.hands?.length ?? 0) > 1',
  },
})
export class SeatView {
  readonly seat = input<Seat | null>(null);
  readonly phase = input.required<GamePhase>();
  /** Position autour de la table (0 = servi en premier), pour l'ordre de distribution. */
  readonly dealRank = input(0);

  readonly kick = output<void>();

  /** Texte affiché à la place des cartes quand le joueur n'a pas de main. */
  protected readonly idleText = computed(() => {
    const seat = this.seat();
    if (!seat) {
      return '';
    }
    if (this.phase() !== 'Betting') {
      return 'Hors manche';
    }
    if (seat.bet === null) {
      return 'Choisit sa mise';
    }
    return seat.bet === 0 ? 'Passe' : '';
  });

  /** Le bilan n'est affiché qu'une fois la manche jouée. */
  protected readonly result = computed(() => {
    const phase = this.phase();
    return phase === 'Distribution' || phase === 'Results' ? (this.seat()?.result ?? null) : null;
  });

  /** « 3 gorgées et 1 cul sec », ou null s'il n'y a rien à boire. */
  protected readonly toDrink = computed(() => {
    const result = this.result();
    return result ? formatToDrink(result.sipsToDrink, result.culsSecsToDrink) : null;
  });
}
