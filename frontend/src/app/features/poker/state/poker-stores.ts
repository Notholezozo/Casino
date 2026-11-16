import { Injectable, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { PokerApi } from '../../../core/api/poker-api';
import { TablesApi } from '../../../core/api/tables-api';
import { DrinkAllocation } from '../../../core/models/blackjack.models';
import { PokerActionRequest, PokerPlayerView, PokerTableView } from '../../../core/models/poker.models';
import { LiveGameStore } from '../../../core/realtime/live-game-store';
import { AdminSession, PlayerSession } from '../../../core/session/session.model';

/** État et commandes de la vue joueur du poker. */
@Injectable()
export class PokerPlayerStore extends LiveGameStore<PokerPlayerView> {
  private readonly pokerApi = inject(PokerApi);
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

  act(request: PokerActionRequest): void {
    this.run(this.pokerApi.act(request));
  }

  distribute(allocations: DrinkAllocation[]): void {
    this.run(this.pokerApi.distribute(allocations));
  }

  leave(): void {
    const session = this.session;
    if (session) {
      this.run(this.tablesApi.removePlayer(session.code, session.playerId), () => this.exit.toHome());
    }
  }

  protected override fetchState(): Observable<PokerPlayerView> {
    return this.pokerApi.getPlayerState();
  }
}

/** État et commandes de la page table (admin) du poker. */
@Injectable()
export class PokerTableStore extends LiveGameStore<PokerTableView> {
  private readonly pokerApi = inject(PokerApi);
  private readonly tablesApi = inject(TablesApi);
  private session: AdminSession | null = null;

  connect(session: AdminSession): void {
    this.session = session;
    this.open(session.token, 'TableState');
  }

  kick(playerId: string): void {
    const session = this.session;
    if (session) {
      this.run(this.tablesApi.removePlayer(session.code, playerId));
    }
  }

  closeTable(): void {
    const session = this.session;
    if (session) {
      this.run(this.tablesApi.close(session.code), () => this.exit.toHome('Table fermée.'));
    }
  }

  protected override fetchState(): Observable<PokerTableView> {
    return this.pokerApi.getTableState();
  }
}
