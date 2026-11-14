import { Component, computed, input } from '@angular/core';

/** Forme minimale d'une carte, commune à tous les jeux. */
export interface PlayingCardFace {
  rank: string;
  suit: 'Clubs' | 'Diamonds' | 'Hearts' | 'Spades';
}

// U+FE0E force le rendu « texte » : sans lui, iOS/Android affichent des emojis.
const SUIT_SYMBOLS: Record<PlayingCardFace['suit'], string> = {
  Clubs: '♣︎',
  Diamonds: '♦︎',
  Hearts: '♥︎',
  Spades: '♠︎',
};

const SUIT_NAMES: Record<PlayingCardFace['suit'], string> = {
  Clubs: 'trèfle',
  Diamonds: 'carreau',
  Hearts: 'cœur',
  Spades: 'pique',
};

/**
 * Carte à jouer dessinée en CSS. Sans `card`, affiche le dos.
 * La taille suit la variable CSS `--card-w` du conteneur.
 */
@Component({
  selector: 'app-playing-card',
  templateUrl: './playing-card.html',
  styleUrl: './playing-card.scss',
  host: {
    role: 'img',
    '[class.is-red]': 'isRed()',
    '[class.is-back]': '!card()',
    '[attr.aria-label]': 'label()',
  },
})
export class PlayingCard {
  readonly card = input<PlayingCardFace | null>(null);

  protected readonly symbol = computed(() => {
    const card = this.card();
    return card ? SUIT_SYMBOLS[card.suit] : '';
  });

  protected readonly isRed = computed(() => {
    const suit = this.card()?.suit;
    return suit === 'Hearts' || suit === 'Diamonds';
  });

  protected readonly label = computed(() => {
    const card = this.card();
    return card ? `${card.rank} de ${SUIT_NAMES[card.suit]}` : 'Carte cachée';
  });
}
