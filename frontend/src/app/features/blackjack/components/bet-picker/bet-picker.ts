import { Component, computed, input, linkedSignal, output } from '@angular/core';
import { BetLimits } from '../../../../core/models/blackjack.models';
import { SipsPipe } from '../../../../shared/pipes/sips-pipe';

/** Choix de la mise en gorgées : on sélectionne un jeton puis on valide, ou on passe (mise 0). */
@Component({
  selector: 'bj-bet-picker',
  imports: [SipsPipe],
  templateUrl: './bet-picker.html',
  styleUrl: './bet-picker.scss',
})
export class BetPicker {
  readonly limits = input.required<BetLimits>();
  /** Mise déjà enregistrée : null = pas encore répondu, 0 = passe. */
  readonly currentBet = input<number | null>(null);
  readonly disabled = input(false);

  readonly betChosen = output<number>();

  /** Sélection locale, réalignée sur la mise enregistrée dès qu'elle change. */
  protected readonly selected = linkedSignal<number | null>(() => this.currentBet());

  protected readonly amounts = computed(() => {
    const { min, max } = this.limits();
    const first = Math.max(min, 1);
    return Array.from({ length: Math.max(max - first + 1, 0) }, (_, index) => first + index);
  });

  protected readonly canConfirm = computed(() => {
    const selected = this.selected();
    return selected !== null && selected > 0 && selected !== this.currentBet();
  });

  protected confirm(): void {
    const selected = this.selected();
    if (selected !== null && selected > 0) {
      this.betChosen.emit(selected);
    }
  }

  protected pass(): void {
    this.betChosen.emit(0);
  }
}
