import { PokerPhase, PokerSeat, PokerTableView } from '../../core/models/poker.models';

// Comme au blackjack, le vocabulaire du jeu reste en anglais : Fold, Check, Call, Raise, Flop…

const STREET_NAMES: Partial<Record<PokerPhase, string>> = {
  PreFlop: 'Pre-flop',
  Flop: 'Flop',
  Turn: 'Turn',
  River: 'River',
};

export function isBettingPhase(phase: PokerPhase): boolean {
  return phase in STREET_NAMES;
}

/** Phrase d'état commune aux deux écrans : où en est la main, et qui doit parler. */
export function describePokerPhase(view: PokerTableView): string {
  switch (view.phase) {
    case 'Waiting': {
      const ready = view.seats.filter((seat) => seat.isConnected).length;
      return ready < 2 ? 'Il faut deux joueurs pour distribuer' : 'Nouvelle main…';
    }
    case 'Distribution':
      return 'Le gagnant distribue le pot';
    case 'Results':
      return 'Fin de la main';
    default: {
      const active = view.seats.find((seat) => seat.playerId === view.activePlayerId);
      const street = STREET_NAMES[view.phase];
      return active ? `${street} · au tour de ${active.name}` : `${street}`;
    }
  }
}

/** Rôle du joueur pour la main en cours : bouton du donneur, petite ou grosse blinde. */
export function seatRole(seat: PokerSeat): string | null {
  if (seat.isButton) {
    return seat.isSmallBlind ? 'D · SB' : 'D';
  }
  if (seat.isSmallBlind) {
    return 'SB';
  }
  return seat.isBigBlind ? 'BB' : null;
}
