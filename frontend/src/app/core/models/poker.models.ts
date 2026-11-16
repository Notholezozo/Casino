// Miroir de Casino.Application.Poker.Contracts et Casino.Domain.Poker (backend).
// Les cartes, les transferts et les cumuls de gorgées sont communs aux jeux.

import { Card, DrinkTotals, DrinkTransfer } from './blackjack.models';
import { LiveState } from './table.models';

export type PokerPhase =
  | 'Waiting'
  | 'PreFlop'
  | 'Flop'
  | 'Turn'
  | 'River'
  | 'Distribution'
  | 'Results';

export type PokerAction = 'Fold' | 'Check' | 'Call' | 'Raise';

// ---------- Commandes ----------

export interface PokerActionRequest {
  action: PokerAction;
  /** Pour une relance : gorgées ajoutées à l'enchère en cours. */
  raiseBy?: number;
}

// ---------- Vues ----------

export interface PokerSeat {
  playerId: string;
  name: string;
  isConnected: boolean;
  inHand: boolean;
  folded: boolean;
  /** Vrai si le joueur a des cartes en main, visibles ou non. */
  hasCards: boolean;
  /** Ses deux cartes, ou null si on n'a pas le droit de les voir. */
  cards: Card[] | null;
  /** Gorgées misées pendant le tour d'enchères en cours. */
  streetBet: number;
  /** Gorgées misées depuis le début de la main. */
  totalBet: number;
  isActive: boolean;
  isButton: boolean;
  isSmallBlind: boolean;
  isBigBlind: boolean;
  isWinner: boolean;
  /** Nom de sa main ("Pair", "Flush"…), connu à l'abattage. */
  handName: string | null;
  /** Part du pot à distribuer, pour un gagnant. */
  toDistribute: number;
  distributionDone: boolean;
  /** Gorgées reçues des gagnants pendant cette main. */
  sipsToDrink: number;
  totals: DrinkTotals;
}

export interface PokerLimits {
  smallBlind: number;
  bigBlind: number;
  minRaise: number;
  maxRaise: number;
}

/** État de l'écran table. Visible de tous : les cartes privées n'y figurent qu'à l'abattage. */
export interface PokerTableView extends LiveState {
  tableCode: string;
  phase: PokerPhase;
  handNumber: number;
  /** Cartes communes déjà posées (0, 3, 4 ou 5). */
  board: Card[];
  pot: number;
  /** Enchère à égaler pendant le tour en cours. */
  currentBet: number;
  seats: PokerSeat[];
  activePlayerId: string | null;
  maxPlayers: number;
  limits: PokerLimits;
  /** Vrai si la main est allée à l'abattage. */
  showdown: boolean;
  /** Vrai du règlement d'une main jusqu'à la donne suivante. */
  resultsAvailable: boolean;
  transfers: DrinkTransfer[];
}

/** Ce qui ne concerne que le joueur qui regarde : ses options du moment. */
export interface PokerSelf {
  id: string;
  isMyTurn: boolean;
  allowedActions: PokerAction[];
  /** Gorgées à ajouter pour suivre (0 s'il peut checker). */
  amountToCall: number;
  /** Faux s'il n'y a personne à qui distribuer. */
  canDistribute: boolean;
}

/** État envoyé à un joueur : la table, ses propres cartes et ses options. */
export interface PokerPlayerView extends PokerTableView {
  me: PokerSelf;
}
