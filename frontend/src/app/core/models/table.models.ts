// Miroir de Casino.Application.Tables.Contracts (backend).

/** Jeux proposés à la création d'une table. */
export type GameKind = 'Blackjack' | 'Poker';

/** Réglages choisis par le créateur de la table. */
export interface CreateTableRequest {
  game: GameKind;
  /** Mise minimale en gorgées. Au poker : la petite blinde (la grosse vaut le double). */
  minBet: number;
  /** Mise maximale en gorgées. Au poker : la plus grosse relance possible. */
  maxBet: number;
  /** Blackjack seulement : autorise la mise « cul sec », au-dessus de la mise maximale. */
  allowCulSec: boolean;
}

export interface CreateTableResponse {
  code: string;
  adminToken: string;
  game: GameKind;
}

export interface TableSummary {
  code: string;
  game: GameKind;
  playerCount: number;
  maxPlayers: number;
  minBet: number;
  maxBet: number;
  allowCulSec: boolean;
}

/** Adresses IPv4 du serveur sur le réseau local, la plus probable en premier. */
export interface ServerNetwork {
  lanAddresses: string[];
}

export interface JoinTableRequest {
  name: string;
}

export interface JoinTableResponse {
  code: string;
  playerId: string;
  name: string;
  token: string;
  game: GameKind;
}

/**
 * Minimum commun aux vues poussées par le serveur, quel que soit le jeu :
 * de quoi ignorer un état périmé et afficher le compte à rebours.
 */
export interface LiveState {
  /** Compteur croissant : on ignore tout état plus ancien que le dernier reçu. */
  version: number;
  phase: string;
  /** Relatif à l'instant de réception ; null = pas de minuteur. */
  remainingMs: number | null;
}
