import { Component, input } from '@angular/core';
import { RoundResult } from '../../../../core/models/blackjack.models';
import { SipsPipe } from '../../../../shared/pipes/sips-pipe';

/** Bilan d'un joueur pour la manche : ce qu'il boit et pourquoi. */
@Component({
  selector: 'bj-round-result',
  imports: [SipsPipe],
  templateUrl: './round-result.html',
  styleUrl: './round-result.scss',
})
export class RoundResultView {
  readonly result = input.required<RoundResult>();
}
