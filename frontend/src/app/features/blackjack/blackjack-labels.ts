import { Hand, PlayerAction } from '../../core/models/blackjack.models';

export type BadgeTone = 'success' | 'danger' | 'gold' | 'neutral';

export interface Badge {
  label: string;
  tone: BadgeTone;
}

export const ACTION_LABELS: Record<PlayerAction, string> = {
  Hit: 'Tirer',
  Stand: 'Rester',
  Double: 'Doubler',
  Split: 'Séparer',
};

const OUTCOME_BADGES: Record<NonNullable<Hand['outcome']>, Badge> = {
  Win: { label: 'Gagné', tone: 'success' },
  Blackjack: { label: 'Blackjack !', tone: 'gold' },
  Push: { label: 'Égalité', tone: 'neutral' },
  Lose: { label: 'Perdu', tone: 'danger' },
};

const STATUS_BADGES: Record<Hand['status'], Badge | null> = {
  Playing: null,
  Stood: { label: 'Reste', tone: 'neutral' },
  Doubled: { label: 'Doublé', tone: 'neutral' },
  Bust: { label: 'Sauté', tone: 'danger' },
  Blackjack: { label: 'Blackjack', tone: 'gold' },
};

/** Pastille d'une main : le résultat une fois connu, sinon son statut. */
export function handBadge(hand: Hand): Badge | null {
  return hand.outcome ? OUTCOME_BADGES[hand.outcome] : STATUS_BADGES[hand.status];
}

/** Total affiché : une main souple en jeu montre ses deux valeurs (ex. « 7 / 17 »). */
export function totalLabel(total: number, isSoft: boolean): string {
  return isSoft && total < 21 ? `${total - 10} / ${total}` : `${total}`;
}
