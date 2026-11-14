import { Component, computed, input } from '@angular/core';
import { Dealer } from '../../../../core/models/blackjack.models';
import { PlayingCard } from '../../../../shared/ui/playing-card/playing-card';
import { totalLabel } from '../../blackjack-labels';

/** Main du croupier, avec sa carte cachée tant qu'elle n'est pas retournée. */
@Component({
  selector: 'bj-dealer-hand',
  imports: [PlayingCard],
  templateUrl: './dealer-hand.html',
  styleUrl: './dealer-hand.scss',
})
export class DealerHand {
  readonly dealer = input.required<Dealer>();

  protected readonly hasCards = computed(
    () => this.dealer().cards.length > 0 || this.dealer().hasHiddenCard,
  );
  protected readonly total = computed(() => totalLabel(this.dealer().total, this.dealer().isSoft));
}
