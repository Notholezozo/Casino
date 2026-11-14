import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Observable, Subject, filter, map } from 'rxjs';
import { PlayerView, TableView } from '../models/blackjack.models';

/** Événements poussés par le serveur sur /hubs/game (le client n'invoque rien). */
export interface GameHubEvents {
  PlayerState: PlayerView;
  TableState: TableView;
  TableClosed: void;
  RemovedFromTable: void;
}

export type HubStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

type HubEventName = keyof GameHubEvents;

interface HubMessage {
  name: HubEventName;
  payload: unknown;
}

const HUB_URL = '/hubs/game';
const EVENT_NAMES: readonly HubEventName[] = [
  'PlayerState',
  'TableState',
  'TableClosed',
  'RemovedFromTable',
];
const START_RETRY_MS = 2000;
const MAX_RECONNECT_DELAY_MS = 10_000;

/**
 * Connexion temps réel au serveur de jeu. Une seule connexion à la fois ;
 * le serveur pousse l'état courant à chaque (re)connexion.
 */
@Injectable({ providedIn: 'root' })
export class GameHub {
  private connection: HubConnection | null = null;
  private readonly messages = new Subject<HubMessage>();
  private readonly startFailures = new Subject<void>();
  private readonly currentStatus = signal<HubStatus>('disconnected');

  readonly status = this.currentStatus.asReadonly();

  /** Émet quand une tentative de connexion initiale échoue (jeton refusé ou serveur injoignable). */
  readonly startFailed$ = this.startFailures.asObservable();

  on<K extends HubEventName>(name: K): Observable<GameHubEvents[K]> {
    return this.messages.pipe(
      filter((message) => message.name === name),
      map((message) => message.payload as GameHubEvents[K]),
    );
  }

  connect(token: string): void {
    this.disconnect();

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => token })
      .withAutomaticReconnect({
        // Ne jamais abandonner : un téléphone peut rester en veille longtemps.
        nextRetryDelayInMilliseconds: (context) =>
          Math.min(1000 * 2 ** context.previousRetryCount, MAX_RECONNECT_DELAY_MS),
      })
      .configureLogging(LogLevel.Warning)
      .build();

    for (const name of EVENT_NAMES) {
      connection.on(name, (payload: unknown) => this.messages.next({ name, payload }));
    }
    connection.onreconnecting(() => this.setStatus(connection, 'reconnecting'));
    connection.onreconnected(() => this.setStatus(connection, 'connected'));
    connection.onclose(() => this.setStatus(connection, 'disconnected'));

    this.connection = connection;
    void this.start(connection);
  }

  disconnect(): void {
    const connection = this.connection;
    this.connection = null;
    this.currentStatus.set('disconnected');
    void connection?.stop();
  }

  private async start(connection: HubConnection): Promise<void> {
    this.setStatus(connection, 'connecting');
    while (this.connection === connection) {
      try {
        await connection.start();
        this.setStatus(connection, 'connected');
        return;
      } catch {
        if (this.connection !== connection) {
          return;
        }
        this.startFailures.next();
        await new Promise((resolve) => setTimeout(resolve, START_RETRY_MS));
      }
    }
  }

  /** Ignore les callbacks d'une connexion qui a été remplacée entre-temps. */
  private setStatus(connection: HubConnection, status: HubStatus): void {
    if (this.connection === connection) {
      this.currentStatus.set(status);
    }
  }
}
