import { Component, computed, input, output, signal } from '@angular/core';
import { DrinkAllocation, DrinkUnit, OtherPlayer } from '../../../../core/models/blackjack.models';
import { DrinksPipe } from '../../../../shared/pipes/drinks-pipe';

/**
 * Répartition de ce que le joueur a gagné (gorgées ou culs secs) entre les autres joueurs ;
 * la somme doit être exacte.
 */
@Component({
  selector: 'bj-distribution-form',
  imports: [DrinksPipe],
  templateUrl: './distribution-form.html',
  styleUrl: './distribution-form.scss',
})
export class DistributionForm {
  readonly total = input.required<number>();
  readonly unit = input<DrinkUnit>('Sip');
  readonly players = input.required<readonly OtherPlayer[]>();
  readonly disabled = input(false);

  readonly submitted = output<DrinkAllocation[]>();

  private readonly amounts = signal<Readonly<Record<string, number>>>({});

  /** Une ligne par joueur encore présent (un joueur parti perd sa part). */
  protected readonly rows = computed(() =>
    this.players().map((player) => ({ player, amount: this.amounts()[player.id] ?? 0 })),
  );

  protected readonly remaining = computed(
    () => this.total() - this.rows().reduce((sum, row) => sum + row.amount, 0),
  );

  /** « une gorgée » ou « un cul sec », pour les libellés des boutons. */
  protected readonly one = computed(() => (this.unit() === 'CulSec' ? 'un cul sec' : 'une gorgée'));

  protected change(playerId: string, delta: number): void {
    const current = this.amounts()[playerId] ?? 0;
    const next = Math.min(Math.max(current + delta, 0), current + Math.max(this.remaining(), 0));
    this.amounts.update((amounts) => ({ ...amounts, [playerId]: next }));
  }

  protected giveRemaining(playerId: string): void {
    this.change(playerId, this.remaining());
  }

  protected submit(): void {
    if (this.remaining() !== 0) {
      return;
    }
    const allocations = this.rows()
      .filter((row) => row.amount > 0)
      .map((row) => ({ playerId: row.player.id, amount: row.amount }));
    this.submitted.emit(allocations);
  }
}
