import { Component, computed, input } from '@angular/core';
import { Hand } from '../../../../core/models/blackjack.models';
import { SipsPipe } from '../../../../shared/pipes/sips-pipe';
import { PlayingCard } from '../../../../shared/ui/playing-card/playing-card';
import { handBadge, totalLabel } from '../../blackjack-labels';

/** Une main de joueur : cartes en éventail, total, mise et statut/résultat. */
@Component({
  selector: 'bj-hand',
  imports: [PlayingCard, SipsPipe],
  templateUrl: './hand.html',
  styleUrl: './hand.scss',
  host: {
    '[class.is-active]': 'hand().isActive',
    '[class.is-lost]': 'hand().outcome === "Lose" || hand().status === "Bust"',
  },
})
export class HandView {
  readonly hand = input.required<Hand>();

  protected readonly total = computed(() => totalLabel(this.hand().total, this.hand().isSoft));
  protected readonly badge = computed(() => handBadge(this.hand()));
}
