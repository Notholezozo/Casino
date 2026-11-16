import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ServerNetwork } from '../models/table.models';

/** Appels au ServerController : informations sur la machine qui héberge le jeu. */
@Injectable({ providedIn: 'root' })
export class ServerApi {
  private readonly http = inject(HttpClient);

  getNetwork(): Observable<ServerNetwork> {
    return this.http.get<ServerNetwork>('/api/server/network');
  }
}
