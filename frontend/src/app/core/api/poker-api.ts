import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { DistributeRequest, DrinkAllocation } from '../models/blackjack.models';
import { PokerActionRequest, PokerPlayerView, PokerTableView } from '../models/poker.models';

/** Appels au PokerController. La table et le joueur sont déduits du jeton Bearer. */
@Injectable({ providedIn: 'root' })
export class PokerApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/poker';

  /** État courant vu par un joueur (à appeler avec un jeton joueur). */
  getPlayerState(): Observable<PokerPlayerView> {
    return this.http.get<PokerPlayerView>(`${this.baseUrl}/state`);
  }

  /** État courant de toute la table (à appeler avec un jeton admin). */
  getTableState(): Observable<PokerTableView> {
    return this.http.get<PokerTableView>(`${this.baseUrl}/state`);
  }

  /** Fold, Check, Call ou Raise. */
  act(request: PokerActionRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/action`, request);
  }

  /** Le gagnant répartit sa part du pot. */
  distribute(allocations: DrinkAllocation[]): Observable<void> {
    const body: DistributeRequest = { allocations };
    return this.http.post<void>(`${this.baseUrl}/distribution`, body);
  }
}
