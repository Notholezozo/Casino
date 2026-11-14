import { Component, computed, input, output, signal } from '@angular/core';
import { OtherPlayer, SipAllocation } from '../../../../core/models/blackjack.models';
import { SipsPipe } from '../../../../shared/pipes/sips-pipe';

/** Répartition des gorgées gagnées entre les autres joueurs ; la somme doit être exacte. */
@Component({
  selector: 'bj-distribution-form',
  imports: [SipsPipe],
  templateUrl: './distribution-form.html',
  styleUrl: './distribution-form.scss',
})
export class DistributionForm {
  readonly total = input.required<number>();
  readonly players = input.required<readonly OtherPlayer[]>();
  readonly disabled = input(false);

  readonly submitted = output<SipAllocation[]>();

  private readonly amounts = signal<Readonly<Record<string, number>>>({});

  /** Une ligne par joueur encore présent (un joueur parti perd sa part). */
  protected readonly rows = computed(() =>
    this.players().map((player) => ({ player, sips: this.amounts()[player.id] ?? 0 })),
  );

  protected readonly remaining = computed(
    () => this.total() - this.rows().reduce((sum, row) => sum + row.sips, 0),
  );

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
      .filter((row) => row.sips > 0)
      .map((row) => ({ playerId: row.player.id, sips: row.sips }));
    this.submitted.emit(allocations);
  }
}
