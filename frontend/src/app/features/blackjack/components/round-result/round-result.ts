import { Component, computed, input } from '@angular/core';
import { RoundResult } from '../../../../core/models/blackjack.models';
import { DrinksPipe, formatToDrink } from '../../../../shared/pipes/drinks-pipe';

/** Bilan d'un joueur pour la manche : ce qu'il boit et pourquoi. */
@Component({
  selector: 'bj-round-result',
  imports: [DrinksPipe],
  templateUrl: './round-result.html',
  styleUrl: './round-result.scss',
})
export class RoundResultView {
  readonly result = input.required<RoundResult>();

  /** « 3 gorgées et 1 cul sec », ou null s'il n'y a rien à boire. */
  protected readonly toDrink = computed(() =>
    formatToDrink(this.result().sipsToDrink, this.result().culsSecsToDrink),
  );
}
