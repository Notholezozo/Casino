import { Component, computed, input } from '@angular/core';
import { Card } from '../../../core/models/blackjack.models';
import { DrinksPipe } from '../../../shared/pipes/drinks-pipe';
import { PlayingCard } from '../../../shared/ui/playing-card/playing-card';

const BOARD_SIZE = 5;
/** Les cartes communes sont servies après les cartes privées de tous les joueurs. */
const BOARD_DEAL_ORDER = 100;

/** Les cartes communes (flop, turn, river) et le pot, devant le croupier. */
@Component({
  selector: 'pk-board',
  imports: [DrinksPipe, PlayingCard],
  template: `
    <div class="cards">
      @for (card of board(); track $index) {
        <app-playing-card [card]="card" [dealOrder]="dealOrder + $index" />
      }
      @for (slot of emptySlots(); track slot) {
        <span class="slot" aria-hidden="true"></span>
      }
    </div>
    <p class="pot">
      Pot : <strong>{{ pot() | drinks }}</strong>
    </p>
  `,
  styles: `
    :host {
      display: grid;
      justify-items: center;
      gap: var(--space-2);
    }

    .cards {
      display: flex;
      justify-content: center;
      gap: calc(var(--card-w) * 0.1);
    }

    /* Emplacement d'une carte commune pas encore retournée. */
    .slot {
      width: var(--card-w);
      aspect-ratio: 5 / 7;
      border: 2px dashed var(--on-felt);
      border-radius: var(--radius-lg);
      opacity: 0.35;
    }

    .pot {
      padding: 2px var(--space-3);
      border-radius: var(--radius-sm);
      background: var(--on-felt);
      color: var(--felt-deep);
      font: 400 14px/20px var(--font-sans);
      white-space: nowrap;

      strong {
        font: 700 16px/20px var(--font-display);
        font-variant-numeric: tabular-nums;
      }
    }
  `,
})
export class Board {
  readonly board = input.required<readonly Card[]>();
  readonly pot = input.required<number>();

  protected readonly dealOrder = BOARD_DEAL_ORDER;

  protected readonly emptySlots = computed(() =>
    Array.from({ length: Math.max(BOARD_SIZE - this.board().length, 0) }, (_, index) => index),
  );
}
