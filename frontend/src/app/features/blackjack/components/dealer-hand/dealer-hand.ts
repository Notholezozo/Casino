import { Component, computed, inject, input, signal } from '@angular/core';
import { Card, Dealer } from '../../../../core/models/blackjack.models';
import { DealSequencer } from '../../../../shared/ui/dealing/deal-sequencer';
import { PlayingCard } from '../../../../shared/ui/playing-card/playing-card';
import { DEALER_LABEL, totalLabel } from '../../blackjack-labels';

/** Le croupier est servi après les joueurs : son rang de distribution est le plus élevé. */
const DEALER_DEAL_RANK = 9;

/** Main du dealer, avec sa carte cachée tant qu'elle n'est pas retournée. */
@Component({
  selector: 'bj-dealer-hand',
  imports: [PlayingCard],
  templateUrl: './dealer-hand.html',
  styleUrl: './dealer-hand.scss',
  host: {
    '[class.is-dealing]': 'dealing()',
  },
})
export class DealerHand {
  /** Vrai pendant que la donne est en vol : le total attend que les cartes soient posées. */
  protected readonly dealing = inject(DealSequencer, { optional: true })?.dealing ?? signal(false);

  readonly dealer = input.required<Dealer>();

  protected readonly label = DEALER_LABEL;
  protected readonly dealRank = DEALER_DEAL_RANK;

  /**
   * Emplacements de cartes : les cartes visibles, puis un dos (null) pour la carte cachée.
   * Quand elle est révélée, le dos reçoit sa face au même emplacement et se retourne.
   */
  protected readonly slots = computed<(Card | null)[]>(() => {
    const { cards, hasHiddenCard } = this.dealer();
    return hasHiddenCard ? [...cards, null] : cards;
  });

  protected readonly total = computed(() => {
    const { total, isSoft, hasHiddenCard } = this.dealer();
    return hasHiddenCard ? `${total}+?` : totalLabel(total, isSoft);
  });
}
