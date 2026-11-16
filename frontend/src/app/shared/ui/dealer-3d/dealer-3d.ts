import {
  Component,
  DestroyRef,
  ElementRef,
  NgZone,
  afterNextRender,
  effect,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { DEAL_RELEASE_MS, DealerMood, ViewportPoint } from './dealer-3d.types';
import type { DealerStage } from './dealer-stage';

export type { DealerMood } from './dealer-3d.types';

/**
 * Croupier 3D : une tête de croupier qui sort de toilettes en porcelaine, avec deux mains gantées
 * qui distribuent les cartes. Le modèle est entièrement construit en code (three.js, chargé à la demande).
 *
 * Le composant prend la taille que lui donne son parent (4:3 par défaut) et ne dessine rien si WebGL
 * est indisponible ; `dealTo()` et `releasePoint()` restent alors utilisables.
 */
@Component({
  selector: 'app-dealer-3d',
  template: `<canvas #canvas aria-hidden="true"></canvas>`,
  styles: `
    :host {
      display: block;
      position: relative;
      aspect-ratio: 4 / 3;
    }

    /* Sans WebGL on garde la place dans la mise en page : les cartes partent toujours du même endroit. */
    :host(.is-unavailable) canvas {
      visibility: hidden;
    }

    canvas {
      position: absolute;
      inset: 0;
      display: block;
      width: 100%;
      height: 100%;
    }
  `,
  host: { '[class.is-unavailable]': 'unavailable()' },
})
export class Dealer3d {
  /** Délai entre l'appel à dealTo() et l'instant où la carte quitte la main. */
  static readonly RELEASE_MS = DEAL_RELEASE_MS;

  /** Version téléphone : plus petite, moins coûteuse (pixel ratio plafonné plus bas, sans ombres). */
  readonly compact = input(false);
  readonly mood = input<DealerMood>('idle');

  protected readonly unavailable = signal(false);

  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly host: HTMLElement = inject(ElementRef).nativeElement;
  private readonly zone = inject(NgZone);
  private readonly stage = signal<DealerStage | null>(null);
  private destroyed = false;

  constructor() {
    const teardown: (() => void)[] = [];

    inject(DestroyRef).onDestroy(() => {
      this.destroyed = true;
      teardown.forEach((undo) => undo());
      this.stage()?.dispose();
    });

    afterNextRender(() => void this.start(teardown));

    effect(() => {
      const stage = this.stage();
      if (stage) {
        stage.setCompact(this.compact());
        stage.setMood(this.mood());
      }
    });
  }

  /**
   * Joue le geste de distribution : la main droite prend une carte du paquet tenu par la gauche
   * et la lance. Des appels rapprochés s'enchaînent : chaque geste repart de la pose en cours.
   * @param direction -1 = vers la gauche de l'écran, 1 = vers la droite, 0 = droit devant.
   */
  dealTo(direction: number): void {
    const clamped = Math.min(1, Math.max(-1, Number.isFinite(direction) ? direction : 0));
    this.stage()?.dealTo(clamped);
  }

  /**
   * Coordonnées viewport (px) du point où la carte quitte la main, pour la direction du dernier
   * dealTo() : de là peut partir une carte de la page. Repli : centre bas du composant.
   */
  releasePoint(): ViewportPoint {
    const stage = this.stage();
    if (stage) {
      return stage.releasePoint();
    }
    const rect = this.host.getBoundingClientRect();
    return { x: rect.left + rect.width / 2, y: rect.top + rect.height * 0.8 };
  }

  private async start(teardown: (() => void)[]): Promise<void> {
    const canvas = this.canvas().nativeElement;

    // On demande le contexte nous-mêmes : s'il est refusé, on s'arrête là sans erreur dans la console.
    const context = canvas.getContext('webgl2', { alpha: true, antialias: true });
    if (!context) {
      this.unavailable.set(true);
      return;
    }

    let stage: DealerStage;
    try {
      const { DealerStage } = await import('./dealer-stage');
      if (this.destroyed) {
        return;
      }
      const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
      // Hors zone Angular : la boucle de rendu ne doit déclencher aucune détection de changements.
      stage = this.zone.runOutsideAngular(
        () =>
          new DealerStage(canvas, context, {
            compact: this.compact(),
            reducedMotion: reducedMotion.matches,
          }),
      );
      this.zone.runOutsideAngular(() => this.observe(stage, reducedMotion, teardown));
    } catch {
      this.unavailable.set(true);
      return;
    }

    this.stage.set(stage);
  }

  /** Suit la taille, la visibilité et la préférence de mouvement, et les répercute sur la scène. */
  private observe(stage: DealerStage, reducedMotion: MediaQueryList, teardown: (() => void)[]): void {
    const resize = new ResizeObserver(() => stage.resize(this.host.clientWidth, this.host.clientHeight));
    resize.observe(this.host);
    teardown.push(() => resize.disconnect());

    // La boucle ne tourne que si le croupier est à l'écran et l'onglet au premier plan.
    let onScreen = false;
    const syncRunning = () => stage.setRunning(onScreen && !document.hidden);

    const visibility = new IntersectionObserver((entries) => {
      onScreen = entries.some((entry) => entry.isIntersecting);
      syncRunning();
    });
    visibility.observe(this.host);
    teardown.push(() => visibility.disconnect());

    document.addEventListener('visibilitychange', syncRunning);
    teardown.push(() => document.removeEventListener('visibilitychange', syncRunning));

    const onMotionPreference = () => stage.setReducedMotion(reducedMotion.matches);
    reducedMotion.addEventListener('change', onMotionPreference);
    teardown.push(() => reducedMotion.removeEventListener('change', onMotionPreference));
  }
}
