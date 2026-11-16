import { Injectable, OnDestroy, signal } from '@angular/core';

/** Ce que le séquenceur attend d'un croupier animé (le composant 3D le fournit). */
export interface DealerHandle {
  /** Joue le geste de distribution ; direction ∈ [-1, 1], de la gauche à la droite de l'écran. */
  dealTo(direction: number): void;
  /** Point de l'écran (px, repère viewport) où la carte quitte la main. */
  releasePoint(): { x: number; y: number };
  /** Délai entre l'appel à `dealTo` et l'instant où la carte quitte la main. */
  readonly releaseDelayMs: number;
}

interface PendingCard {
  element: HTMLElement;
  order: number;
}

/** Écart entre deux cartes distribuées à la suite. */
const STAGGER_MS = 170;
/** Durée du vol d'une carte, du croupier jusqu'à sa place. */
const FLIGHT_MS = 380;
/** Délai maximal avant de lancer les cartes en attente si aucune image n'est dessinée. */
const FLUSH_FALLBACK_MS = 120;
/** À partir de ce nombre de cartes apparues ensemble, c'est une donne et non un simple tirage. */
const ROUND_DEAL_MIN_CARDS = 3;

/**
 * Anime la distribution : chaque carte qui apparaît à l'écran part de la main du croupier
 * et glisse jusqu'à sa place, l'une après l'autre.
 *
 * Le serveur envoie toute la donne en un seul état ; c'est ici qu'elle est remise en séquence.
 * À fournir au niveau de la page : une page = une table = un croupier.
 */
@Injectable()
export class DealSequencer implements OnDestroy {
  /**
   * Vrai pendant qu'une donne complète est en vol. Les mains s'en servent pour n'afficher
   * leur total qu'une fois les cartes posées. Une carte isolée (hit) ne le déclenche pas.
   */
  readonly dealing = signal(false);

  private dealer: DealerHandle | null = null;
  private dealingUntil = 0;
  private pending: PendingCard[] = [];
  private frame = 0;
  private armed = false;
  /** Instant (performance.now) jusqu'auquel des cartes déjà planifiées sont encore en vol. */
  private busyUntil = 0;
  private readonly timers = new Set<ReturnType<typeof setTimeout>>();
  private readonly reducedMotion = matchMedia('(prefers-reduced-motion: reduce)').matches;

  /** Branche (ou débranche) le croupier animé. Sans lui, les cartes arrivent du haut de l'écran. */
  setDealer(dealer: DealerHandle | null): void {
    this.dealer = dealer;
  }

  /**
   * Active les animations. À appeler une fois le premier état affiché : les cartes déjà sur
   * la table au chargement de la page ne doivent pas être « redistribuées ».
   */
  arm(): void {
    this.armed = true;
  }

  /**
   * Signale qu'une carte vient d'apparaître. `order` classe les cartes d'une même donne :
   * plus il est petit, plus la carte est servie tôt.
   */
  enqueue(element: HTMLElement, order: number): void {
    if (!this.armed || this.reducedMotion) {
      return;
    }

    // Invisible jusqu'à son départ, pour ne pas apparaître une image à sa place finale.
    element.style.opacity = '0';
    this.pending.push({ element, order });

    // Dès qu'une donne se dessine, les totaux se cachent — sans attendre le départ des cartes.
    if (this.pending.length >= ROUND_DEAL_MIN_CARDS) {
      this.dealing.set(true);
    }

    if (!this.frame) {
      this.frame = requestAnimationFrame(() => this.flush());
      // Dans un onglet en arrière-plan, le navigateur ne dessine plus d'image : sans ce
      // minuteur de secours, les cartes resteraient invisibles jusqu'au retour sur l'onglet.
      this.later(FLUSH_FALLBACK_MS, () => this.flush());
    }
  }

  ngOnDestroy(): void {
    cancelAnimationFrame(this.frame);
    this.timers.forEach((timer) => clearTimeout(timer));
  }

  private flush(): void {
    cancelAnimationFrame(this.frame);
    this.frame = 0;
    if (this.pending.length === 0) {
      return;
    }

    const cards = this.pending.sort((a, b) => a.order - b.order);
    this.pending = [];

    const now = performance.now();
    let start = Math.max(this.busyUntil - now, 0);

    for (const { element } of cards) {
      element.style.opacity = '';
      if (!element.isConnected) {
        continue;
      }

      const rect = element.getBoundingClientRect();
      const centerX = rect.left + rect.width / 2;
      const centerY = rect.top + rect.height / 2;
      const origin = this.dealer?.releasePoint() ?? { x: innerWidth / 2, y: -rect.height };
      const dealer = this.dealer;

      if (dealer) {
        const direction = Math.max(-1, Math.min(1, (centerX - origin.x) / (innerWidth / 2)));
        this.later(start, () => dealer.dealTo(direction));
      }

      element.animate(
        [
          {
            opacity: 0,
            transform: `translate(${origin.x - centerX}px, ${origin.y - centerY}px) scale(0.45) rotate(-28deg)`,
          },
          { opacity: 1, offset: 0.12 },
          { opacity: 1, transform: 'none' },
        ],
        {
          duration: FLIGHT_MS,
          delay: start + (dealer?.releaseDelayMs ?? 0),
          easing: 'cubic-bezier(0.2, 0.7, 0.2, 1)',
          // « backwards » : la première image (invisible) s'applique pendant l'attente.
          fill: 'backwards',
        },
      );

      start += STAGGER_MS;
    }

    this.busyUntil = now + start;

    if (cards.length >= ROUND_DEAL_MIN_CARDS) {
      this.announceDealing(start + (this.dealer?.releaseDelayMs ?? 0) + FLIGHT_MS);
    }
  }

  /** Lève `dealing` pour la durée donnée ; une donne qui s'enchaîne prolonge la précédente. */
  private announceDealing(durationMs: number): void {
    const until = performance.now() + durationMs;
    this.dealingUntil = Math.max(this.dealingUntil, until);
    this.dealing.set(true);
    this.later(durationMs, () => {
      if (performance.now() >= this.dealingUntil - 1) {
        this.dealing.set(false);
      }
    });
  }

  private later(delayMs: number, action: () => void): void {
    const timer = setTimeout(() => {
      this.timers.delete(timer);
      action();
    }, delayMs);
    this.timers.add(timer);
  }
}
