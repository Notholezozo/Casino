import { Component, booleanAttribute, computed, inject, input, signal } from '@angular/core';
import { Hand } from '../../../../core/models/blackjack.models';
import { DrinksPipe } from '../../../../shared/pipes/drinks-pipe';
import { DealSequencer } from '../../../../shared/ui/dealing/deal-sequencer';
import { PlayingCard } from '../../../../shared/ui/playing-card/playing-card';
import { handBadge, totalLabel } from '../../blackjack-labels';

/** Une main de joueur : cartes qui se chevauchent, total, mise et statut/résultat. */
@Component({
  selector: 'bj-hand',
  imports: [DrinksPipe, PlayingCard],
  templateUrl: './hand.html',
  styleUrl: './hand.scss',
  host: {
    '[class.is-active]': 'hand().isActive',
    '[class.is-lost]': 'hand().outcome === "Lose" || hand().status === "Bust"',
    '[class.is-dealing]': 'dealing()',
    '[class.is-side]': 'sideMeta()',
  },
})
export class HandView {
  /**
   * Place le total et la mise à droite des cartes plutôt que dessous. Sur un téléphone,
   * la largeur ne manque pas mais chaque pixel de hauteur compte.
   */
  readonly sideMeta = input(false, { transform: booleanAttribute });

  /** Vrai pendant que la donne est en vol : total et mise attendent que les cartes soient posées. */
  protected readonly dealing = inject(DealSequencer, { optional: true })?.dealing ?? signal(false);

  readonly hand = input.required<Hand>();
  /**
   * Rang du joueur dans l'ordre de distribution (0 = servi en premier).
   * Le croupier sert une carte à chacun avant de passer à la suivante.
   */
  readonly dealRank = input(0);

  protected readonly total = computed(() => totalLabel(this.hand().total, this.hand().isSoft));
  protected readonly badge = computed(() => handBadge(this.hand()));
}
