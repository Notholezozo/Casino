import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  DistributeRequest,
  PlaceBetRequest,
  PlayerAction,
  PlayerActionRequest,
  PlayerView,
  SipAllocation,
  TableView,
} from '../models/blackjack.models';

/** Appels au BlackjackController. La table et le joueur sont déduits du jeton Bearer. */
@Injectable({ providedIn: 'root' })
export class BlackjackApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/blackjack';

  /** État courant vu par un joueur (à appeler avec un jeton joueur). */
  getPlayerState(): Observable<PlayerView> {
    return this.http.get<PlayerView>(`${this.baseUrl}/state`);
  }

  /** État courant de toute la table (à appeler avec un jeton admin). */
  getTableState(): Observable<TableView> {
    return this.http.get<TableView>(`${this.baseUrl}/state`);
  }

  placeBet(sips: number): Observable<void> {
    const body: PlaceBetRequest = { sips };
    return this.http.post<void>(`${this.baseUrl}/bet`, body);
  }

  act(action: PlayerAction): Observable<void> {
    const body: PlayerActionRequest = { action };
    return this.http.post<void>(`${this.baseUrl}/action`, body);
  }

  distribute(allocations: SipAllocation[]): Observable<void> {
    const body: DistributeRequest = { allocations };
    return this.http.post<void>(`${this.baseUrl}/distribution`, body);
  }
}
