import { Component, computed, input, linkedSignal, output } from '@angular/core';
import { PokerAction, PokerActionRequest, PokerLimits } from '../../../core/models/poker.models';
import { DrinksPipe } from '../../../shared/pipes/drinks-pipe';

/**
 * Les actions d'un joueur de poker : Fold, Check ou Call, et Raise avec le choix du montant.
 * Seules celles autorisées par le serveur sont actives.
 */
@Component({
  selector: 'pk-action-bar',
  imports: [DrinksPipe],
  templateUrl: './poker-action-bar.html',
  styleUrl: './poker-action-bar.scss',
})
export class PokerActionBar {
  readonly allowedActions = input.required<readonly PokerAction[]>();
  /** Gorgées à ajouter pour suivre (0 s'il peut checker). */
  readonly amountToCall = input.required<number>();
  readonly limits = input.required<PokerLimits>();
  readonly disabled = input(false);

  readonly actionChosen = output<PokerActionRequest>();

  /** Montant de la relance, ramené dans les limites de la table si elles changent. */
  protected readonly raiseBy = linkedSignal(() => this.limits().minRaise);

  protected readonly canCheck = computed(() => this.allowedActions().includes('Check'));
  protected readonly canCall = computed(() => this.allowedActions().includes('Call'));
  protected readonly canRaise = computed(() => this.allowedActions().includes('Raise'));
  protected readonly canFold = computed(() => this.allowedActions().includes('Fold'));

  /** Ce que la relance coûte en tout au joueur : suivre, plus le montant ajouté. */
  protected readonly raiseCost = computed(() => this.amountToCall() + this.raiseBy());

  protected changeRaise(delta: number): void {
    const { minRaise, maxRaise } = this.limits();
    this.raiseBy.update((amount) => Math.min(Math.max(amount + delta, minRaise), maxRaise));
  }

  protected choose(action: PokerAction): void {
    this.actionChosen.emit(action === 'Raise' ? { action, raiseBy: this.raiseBy() } : { action });
  }
}
