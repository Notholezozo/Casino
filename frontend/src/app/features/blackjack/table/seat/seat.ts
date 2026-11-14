import { Component, computed, input, output } from '@angular/core';
import { GamePhase, Seat } from '../../../../core/models/blackjack.models';
import { SipsPipe } from '../../../../shared/pipes/sips-pipe';
import { HandView } from '../../components/hand/hand';

/** Un siège de la table vu par l'admin : joueur, mains, mise et bilan. Sans `seat`, siège libre. */
@Component({
  selector: 'bj-seat',
  imports: [HandView, SipsPipe],
  templateUrl: './seat.html',
  styleUrl: './seat.scss',
  host: {
    '[class.is-empty]': '!seat()',
    '[class.is-active]': 'seat()?.isActive',
    '[class.is-offline]': 'seat() && !seat()!.isConnected',
  },
})
export class SeatView {
  readonly seat = input<Seat | null>(null);
  readonly phase = input.required<GamePhase>();

  readonly kick = output<void>();

  /** Texte affiché à la place des cartes quand le joueur n'a pas de main. */
  protected readonly idleText = computed(() => {
    const seat = this.seat();
    if (!seat) {
      return '';
    }
    if (this.phase() !== 'Betting') {
      return 'Attend la prochaine manche';
    }
    if (seat.bet === null) {
      return 'Réfléchit…';
    }
    return seat.bet === 0 ? 'Passe' : '';
  });

  /** Le bilan n'est affiché qu'une fois la manche jouée. */
  protected readonly result = computed(() => {
    const phase = this.phase();
    return phase === 'Distribution' || phase === 'Results' ? (this.seat()?.result ?? null) : null;
  });
}
