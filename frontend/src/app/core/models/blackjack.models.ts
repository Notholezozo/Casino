// Miroir de Casino.Application.Blackjack.Contracts et Casino.Domain.Blackjack (backend).

export type GamePhase = 'Betting' | 'PlayerTurns' | 'DealerTurn' | 'Distribution' | 'Results';
export type HandStatus = 'Playing' | 'Stood' | 'Doubled' | 'Bust' | 'Blackjack';
export type HandOutcome = 'Win' | 'Blackjack' | 'Push' | 'Lose';
export type PlayerAction = 'Hit' | 'Stand' | 'Double' | 'Split';
export type CardSuit = 'Clubs' | 'Diamonds' | 'Hearts' | 'Spades';
/** Unité d'une mise : des gorgées, ou un cul sec (finir son verre), compté à part. */
export type DrinkUnit = 'Sip' | 'CulSec';

// ---------- Commandes ----------

export interface PlaceBetRequest {
  /** Nombre de gorgées ; 0 = passer la manche. Ignoré si `culSec` est vrai. */
  sips: number;
  /** Miser un cul sec, si la table l'autorise. */
  culSec?: boolean;
}

export interface PlayerActionRequest {
  action: PlayerAction;
}

export interface DrinkAllocation {
  playerId: string;
  /** Quantité donnée, dans l'unité de la mise du gagnant. */
  amount: number;
}

export interface DistributeRequest {
  allocations: DrinkAllocation[];
}

// ---------- Vues ----------

export interface Card {
  /** "A", "2" … "10", "J", "Q", "K". */
  rank: string;
  suit: CardSuit;
}

export interface Hand {
  cards: Card[];
  total: number;
  isSoft: boolean;
  /** Quantité misée sur cette main, dans l'unité `unit`. */
  bet: number;
  unit: DrinkUnit;
  status: HandStatus;
  outcome: HandOutcome | null;
  isActive: boolean;
}

export interface Dealer {
  /** Cartes visibles uniquement tant que `hasHiddenCard` est vrai. */
  cards: Card[];
  hasHiddenCard: boolean;
  total: number;
  isSoft: boolean;
  isBlackjack: boolean;
  isBust: boolean;
}

export interface DrinkTransfer {
  fromPlayerId: string;
  fromPlayerName: string;
  toPlayerId: string;
  toPlayerName: string;
  amount: number;
  unit: DrinkUnit;
}

export interface RoundResult {
  /** Unité de `lost` et `toDistribute` : celle de la mise du joueur. */
  unit: DrinkUnit;
  /** Quantité perdue sur ses propres mains. */
  lost: number;
  /** Quantité gagnée, à distribuer aux autres. */
  toDistribute: number;
  /** Faux s'il n'y a personne à qui distribuer. */
  canDistribute: boolean;
  distributionDone: boolean;
  received: DrinkTransfer[];
  given: DrinkTransfer[];
  /** Gorgées à boire : perdues + reçues. */
  sipsToDrink: number;
  /** Culs secs à boire : perdus + reçus. */
  culsSecsToDrink: number;
}

/** Mises acceptées par la table, choisies à sa création. */
export interface BetLimits {
  min: number;
  max: number;
  allowCulSec: boolean;
}

export interface Shoe {
  remaining: number;
  total: number;
}

/** Cumuls d'un joueur depuis son arrivée à la table. */
export interface DrinkTotals {
  sipsDrunk: number;
  sipsGiven: number;
  culsSecsDrunk: number;
  culsSecsGiven: number;
}

export interface OtherPlayer {
  id: string;
  name: string;
  isConnected: boolean;
  inRound: boolean;
}

export interface PlayerSelf {
  id: string;
  name: string;
  /** Quantité misée : null = pas encore répondu, 0 = passe. */
  bet: number | null;
  betUnit: DrinkUnit;
  inRound: boolean;
  hands: Hand[];
  isMyTurn: boolean;
  allowedActions: PlayerAction[];
  result: RoundResult | null;
  totals: DrinkTotals;
}

/** Champs communs aux vues poussées par le serveur. */
export interface LiveView {
  /** Compteur croissant : on ignore tout état plus ancien que le dernier reçu. */
  version: number;
  tableCode: string;
  phase: GamePhase;
  /** Relatif à l'instant de réception ; null = pas de minuteur. */
  remainingMs: number | null;
  roundNumber: number;
  dealer: Dealer;
  activePlayerId: string | null;
  betLimits: BetLimits;
}

export interface PlayerView extends LiveView {
  me: PlayerSelf;
  others: OtherPlayer[];
}

export interface Seat {
  playerId: string;
  name: string;
  isConnected: boolean;
  bet: number | null;
  betUnit: DrinkUnit;
  inRound: boolean;
  hands: Hand[];
  isActive: boolean;
  result: RoundResult | null;
  totals: DrinkTotals;
}

export interface TableView extends LiveView {
  seats: Seat[];
  maxPlayers: number;
  shoe: Shoe;
  transfers: DrinkTransfer[];
}
