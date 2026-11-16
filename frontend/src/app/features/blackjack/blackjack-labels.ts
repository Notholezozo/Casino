import { Hand, PlayerAction } from '../../core/models/blackjack.models';

// Le vocabulaire du jeu reste en anglais, comme à une vraie table : Hit, Stand, Double, Split, Bust…

/** Classe de badge du design system. Sur le tapis, uniquement des badges pleins. */
export type BadgeTone = 'solid' | 'burgundy' | 'felt';

export interface Badge {
  label: string;
  tone: BadgeTone;
}

export const DEALER_LABEL = 'Dealer';

export const ACTION_LABELS: Record<PlayerAction, string> = {
  Hit: 'Hit',
  Stand: 'Stand',
  Double: 'Double',
  Split: 'Split',
};

const OUTCOME_BADGES: Record<NonNullable<Hand['outcome']>, Badge> = {
  Win: { label: 'Win', tone: 'solid' },
  Blackjack: { label: 'Blackjack', tone: 'solid' },
  Push: { label: 'Push', tone: 'felt' },
  Lose: { label: 'Lose', tone: 'burgundy' },
};

const STATUS_BADGES: Record<Hand['status'], Badge | null> = {
  Playing: null,
  Stood: { label: 'Stand', tone: 'felt' },
  Doubled: { label: 'Double', tone: 'felt' },
  Bust: { label: 'Bust', tone: 'burgundy' },
  Blackjack: { label: 'Blackjack', tone: 'solid' },
};

/**
 * Pastille d'une main : le résultat une fois connu, sinon son statut.
 * Une main sautée reste « Bust » même après le règlement : c'est plus parlant que « Lose ».
 */
export function handBadge(hand: Hand): Badge | null {
  if (hand.status === 'Bust') {
    return STATUS_BADGES.Bust;
  }
  return hand.outcome ? OUTCOME_BADGES[hand.outcome] : STATUS_BADGES[hand.status];
}

/** Total affiché : une main souple en jeu montre ses deux valeurs (ex. « 7 / 17 »). */
export function totalLabel(total: number, isSoft: boolean): string {
  return isSoft && total < 21 ? `${total - 10} / ${total}` : `${total}`;
}
