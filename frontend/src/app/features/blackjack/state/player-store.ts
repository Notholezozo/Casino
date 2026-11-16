import { Injectable, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { BlackjackApi } from '../../../core/api/blackjack-api';
import { TablesApi } from '../../../core/api/tables-api';
import {
  DrinkAllocation,
  PlaceBetRequest,
  PlayerAction,
  PlayerView,
} from '../../../core/models/blackjack.models';
import { PlayerSession } from '../../../core/session/session.model';
import { LiveGameStore } from '../../../core/realtime/live-game-store';

/** État et commandes de la vue joueur. */
@Injectable()
export class PlayerStore extends LiveGameStore<PlayerView> {
  private readonly blackjackApi = inject(BlackjackApi);
  private readonly tablesApi = inject(TablesApi);
  private session: PlayerSession | null = null;

  connect(session: PlayerSession): void {
    this.session = session;
    this.hub
      .on('RemovedFromTable')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.exit.toHome('Tu as été retiré de la table.'));
    this.open(session.token, 'PlayerState');
  }

  placeBet(bet: PlaceBetRequest): void {
    this.run(this.blackjackApi.placeBet(bet));
  }

  act(action: PlayerAction): void {
    this.run(this.blackjackApi.act(action));
  }

  distribute(allocations: DrinkAllocation[]): void {
    this.run(this.blackjackApi.distribute(allocations));
  }

  leave(): void {
    const session = this.session;
    if (!session) {
      return;
    }
    this.run(this.tablesApi.removePlayer(session.code, session.playerId), () => this.exit.toHome());
  }

  protected override fetchState(): Observable<PlayerView> {
    return this.blackjackApi.getPlayerState();
  }
}
