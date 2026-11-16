import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateTableRequest,
  CreateTableResponse,
  JoinTableRequest,
  JoinTableResponse,
  TableSummary,
} from '../models/table.models';

/** Appels au TablesController : cycle de vie des tables et des joueurs. */
@Injectable({ providedIn: 'root' })
export class TablesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/tables';

  create(request: CreateTableRequest): Observable<CreateTableResponse> {
    return this.http.post<CreateTableResponse>(this.baseUrl, request);
  }

  getSummary(code: string): Observable<TableSummary> {
    return this.http.get<TableSummary>(this.tableUrl(code));
  }

  /** Ferme la table (jeton admin). */
  close(code: string): Observable<void> {
    return this.http.delete<void>(this.tableUrl(code));
  }

  join(code: string, request: JoinTableRequest): Observable<JoinTableResponse> {
    return this.http.post<JoinTableResponse>(`${this.tableUrl(code)}/players`, request);
  }

  /** Expulse un joueur (jeton admin) ou quitte la table (jeton du joueur lui-même). */
  removePlayer(code: string, playerId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.tableUrl(code)}/players/${encodeURIComponent(playerId)}`,
    );
  }

  private tableUrl(code: string): string {
    return `${this.baseUrl}/${encodeURIComponent(code)}`;
  }
}
