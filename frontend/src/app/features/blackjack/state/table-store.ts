import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BlackjackApi } from '../../../core/api/blackjack-api';
import { TablesApi } from '../../../core/api/tables-api';
import { TableView } from '../../../core/models/blackjack.models';
import { AdminSession } from '../../../core/session/session.model';
import { LiveGameStore } from './live-game-store';

/** État et commandes de la page table (admin). */
@Injectable()
export class TableStore extends LiveGameStore<TableView> {
  private readonly blackjackApi = inject(BlackjackApi);
  private readonly tablesApi = inject(TablesApi);
  private session: AdminSession | null = null;

  connect(session: AdminSession): void {
    this.session = session;
    this.open(session.token, this.hub.on('TableState'));
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

  protected override fetchState(): Observable<TableView> {
    return this.blackjackApi.getTableState();
  }
}
