import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';
import { ServerApi } from '../api/server-api';

export interface InviteLink {
  /** Accueil avec le code de la table prérempli. */
  url: string;
  /** Faux si aucune adresse réseau n'a été trouvée : le lien ne marche alors que sur ce PC. */
  reachable: boolean;
}

const LOOPBACK_HOSTS = ['localhost', '127.0.0.1', '[::1]'];

/**
 * Fabrique le lien (et donc le QR code) que les joueurs ouvrent sur leur téléphone.
 * Si l'écran table est ouvert en « localhost », l'adresse est remplacée par celle du PC
 * sur le réseau local, fournie par le serveur : un téléphone ne peut pas joindre « localhost ».
 */
@Injectable({ providedIn: 'root' })
export class InviteLinkResolver {
  private readonly serverApi = inject(ServerApi);

  resolve(tableCode: string): Observable<InviteLink> {
    const path = `/?code=${encodeURIComponent(tableCode)}`;
    const local: InviteLink = { url: location.origin + path, reachable: false };

    if (!LOOPBACK_HOSTS.includes(location.hostname)) {
      return of({ ...local, reachable: true });
    }

    return this.serverApi.getNetwork().pipe(
      map(({ lanAddresses }) => {
        const address = lanAddresses[0];
        if (!address) {
          return local;
        }
        // Même protocole et même port que la page courante : c'est le même serveur, vu du réseau.
        const port = location.port ? `:${location.port}` : '';
        return { url: `${location.protocol}//${address}${port}${path}`, reachable: true };
      }),
      catchError(() => of(local)),
    );
  }
}
