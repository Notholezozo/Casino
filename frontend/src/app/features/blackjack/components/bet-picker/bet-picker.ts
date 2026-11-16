import { Component, computed, input, linkedSignal, output } from '@angular/core';
import { BetLimits, DrinkUnit, PlaceBetRequest } from '../../../../core/models/blackjack.models';
import { DrinksPipe } from '../../../../shared/pipes/drinks-pipe';

/** Un jeton de gorgées, ou le cul sec. */
type Selection = number | 'CulSec';

/** Couleur de matière du jeton, par palier de mise. */
function chipTier(amount: number): 'ivory' | 'red' | 'green' | 'black' {
  if (amount >= 10) {
    return 'black';
  }
  if (amount >= 5) {
    return 'green';
  }
  return amount >= 2 ? 'red' : 'ivory';
}

/**
 * Choix de la mise : on sélectionne un jeton (ou le cul sec, si la table l'autorise)
 * puis on valide, ou on passe la manche.
 */
@Component({
  selector: 'bj-bet-picker',
  imports: [DrinksPipe],
  templateUrl: './bet-picker.html',
  styleUrl: './bet-picker.scss',
})
export class BetPicker {
  readonly limits = input.required<BetLimits>();
  /** Mise déjà enregistrée : null = pas encore répondu, 0 = passe. */
  readonly currentBet = input<number | null>(null);
  readonly currentUnit = input<DrinkUnit>('Sip');
  readonly disabled = input(false);

  readonly betChosen = output<PlaceBetRequest>();

  /** Mise enregistrée, ramenée à un choix de jeton (null : rien de misé). */
  protected readonly current = computed<Selection | null>(() => {
    const bet = this.currentBet();
    if (!bet) {
      return null;
    }
    return this.currentUnit() === 'CulSec' ? 'CulSec' : bet;
  });

  /** Sélection locale, réalignée sur la mise enregistrée dès qu'elle change. */
  protected readonly selected = linkedSignal<Selection | null>(() => this.current());

  protected readonly chips = computed(() => {
    const { min, max } = this.limits();
    const first = Math.max(min, 1);
    return Array.from({ length: Math.max(max - first + 1, 0) }, (_, index) => {
      const amount = first + index;
      return { amount, tier: chipTier(amount) };
    });
  });

  protected readonly canConfirm = computed(
    () => this.selected() !== null && this.selected() !== this.current(),
  );

  protected confirm(): void {
    const selected = this.selected();
    if (selected === 'CulSec') {
      this.betChosen.emit({ sips: 0, culSec: true });
    } else if (selected !== null) {
      this.betChosen.emit({ sips: selected });
    }
  }

  protected pass(): void {
    this.betChosen.emit({ sips: 0 });
  }
}
