import { Component, ElementRef, afterNextRender, computed, effect, inject, input } from '@angular/core';
import { DealSequencer } from '../dealing/deal-sequencer';

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

const FLIP_MS = 240;

/**
 * Carte à jouer dessinée en CSS. Sans `card`, affiche le dos.
 * La taille suit la variable CSS `--card-w` du conteneur.
 *
 * Si la page fournit un `DealSequencer`, la carte arrive depuis la main du croupier ;
 * et quand un dos reçoit sa face (carte cachée du croupier), elle se retourne.
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
  /** Rang dans la donne (plus petit = servie plus tôt). Null : la carte apparaît sans animation. */
  readonly dealOrder = input<number | null>(null);

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

  constructor() {
    const element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
    const sequencer = inject(DealSequencer, { optional: true });

    afterNextRender(() => {
      const order = this.dealOrder();
      if (order !== null) {
        sequencer?.enqueue(element, order);
      }
    });

    // Retournement : uniquement quand un dos déjà affiché reçoit sa face.
    let wasBack: boolean | null = null;
    effect(() => {
      const isBack = this.card() === null;
      if (wasBack && !isBack && !matchMedia('(prefers-reduced-motion: reduce)').matches) {
        element.animate([{ transform: 'rotateY(90deg)' }, { transform: 'none' }], {
          duration: FLIP_MS,
          easing: 'ease-out',
        });
      }
      wasBack = isBack;
    });
  }
}
