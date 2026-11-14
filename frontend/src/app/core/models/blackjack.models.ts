// Miroir de Casino.Application.Blackjack.Contracts et Casino.Domain.Blackjack (backend).

export type GamePhase = 'Betting' | 'PlayerTurns' | 'DealerTurn' | 'Distribution' | 'Results';
export type HandStatus = 'Playing' | 'Stood' | 'Doubled' | 'Bust' | 'Blackjack';
export type HandOutcome = 'Win' | 'Blackjack' | 'Push' | 'Lose';
export type PlayerAction = 'Hit' | 'Stand' | 'Double' | 'Split';
export type CardSuit = 'Clubs' | 'Diamonds' | 'Hearts' | 'Spades';

// ---------- Commandes ----------

export interface PlaceBetRequest {
  /** 0 = passer la manche. */
  sips: number;
}

export interface PlayerActionRequest {
  action: PlayerAction;
}

export interface SipAllocation {
  playerId: string;
  sips: number;
}

export interface DistributeRequest {
  allocations: SipAllocation[];
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
  bet: number;
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

export interface SipTransfer {
  fromPlayerId: string;
  fromPlayerName: string;
  toPlayerId: string;
  toPlayerName: string;
  sips: number;
}

export interface RoundResult {
  /** Gorgées perdues sur ses propres mains. */
  sipsLost: number;
  /** Gorgées gagnées, à distribuer aux autres. */
  sipsToDistribute: number;
  /** Faux s'il n'y a personne à qui distribuer. */
  canDistribute: boolean;
  distributionDone: boolean;
  received: SipTransfer[];
  given: SipTransfer[];
  /** Total à boire : perdues + reçues. */
  sipsToDrink: number;
}

export interface BetLimits {
  min: number;
  max: number;
}

export interface Shoe {
  remaining: number;
  total: number;
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
  /** null = pas encore répondu, 0 = passe. */
  bet: number | null;
  inRound: boolean;
  hands: Hand[];
  isMyTurn: boolean;
  allowedActions: PlayerAction[];
  result: RoundResult | null;
  totalDrunk: number;
  totalGiven: number;
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
}

export interface PlayerView extends LiveView {
  me: PlayerSelf;
  others: OtherPlayer[];
  betLimits: BetLimits;
}

export interface Seat {
  playerId: string;
  name: string;
  isConnected: boolean;
  bet: number | null;
  inRound: boolean;
  hands: Hand[];
  isActive: boolean;
  result: RoundResult | null;
  totalDrunk: number;
  totalGiven: number;
}

export interface TableView extends LiveView {
  seats: Seat[];
  maxPlayers: number;
  shoe: Shoe;
  transfers: SipTransfer[];
}
