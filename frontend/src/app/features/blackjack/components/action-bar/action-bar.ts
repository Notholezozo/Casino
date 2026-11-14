import { Component, computed, input, output } from '@angular/core';
import { PlayerAction } from '../../../../core/models/blackjack.models';
import { ACTION_LABELS } from '../../blackjack-labels';

const ACTION_ORDER: readonly PlayerAction[] = ['Hit', 'Stand', 'Double', 'Split'];

/** Les quatre actions de jeu ; seules celles autorisées par le serveur sont actives. */
@Component({
  selector: 'bj-action-bar',
  templateUrl: './action-bar.html',
  styleUrl: './action-bar.scss',
})
export class ActionBar {
  readonly allowedActions = input.required<readonly PlayerAction[]>();
  readonly disabled = input(false);

  readonly actionChosen = output<PlayerAction>();

  protected readonly buttons = computed(() =>
    ACTION_ORDER.map((action) => ({
      action,
      label: ACTION_LABELS[action],
      enabled: this.allowedActions().includes(action),
    })),
  );
}
