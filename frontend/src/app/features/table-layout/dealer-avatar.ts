import {
  Component,
  DestroyRef,
  afterNextRender,
  booleanAttribute,
  inject,
  input,
  viewChild,
} from '@angular/core';
import { Dealer3d, DealerMood } from '../../shared/ui/dealer-3d/dealer-3d';
import { DealSequencer } from '../../shared/ui/dealing/deal-sequencer';

/**
 * Le croupier 3D de la table de blackjack. Il se branche sur le séquenceur de distribution
 * de la page : à chaque carte servie, il joue le geste et la carte part de sa main.
 */
@Component({
  selector: 'bj-dealer-avatar',
  imports: [Dealer3d],
  template: `<app-dealer-3d [compact]="compact()" [mood]="mood()" />`,
  styles: `
    :host {
      display: block;
      aspect-ratio: 4 / 3;
    }

    app-dealer-3d {
      display: block;
      width: 100%;
      height: 100%;
    }
  `,
})
export class DealerAvatar {
  /** Version téléphone : plus petite et moins coûteuse. */
  readonly compact = input(false, { transform: booleanAttribute });
  readonly mood = input<DealerMood>('idle');

  private readonly dealer = viewChild.required(Dealer3d);

  constructor() {
    const sequencer = inject(DealSequencer, { optional: true });

    afterNextRender(() => {
      const dealer = this.dealer();
      sequencer?.setDealer({
        dealTo: (direction) => dealer.dealTo(direction),
        releasePoint: () => dealer.releasePoint(),
        releaseDelayMs: Dealer3d.RELEASE_MS,
      });
    });

    inject(DestroyRef).onDestroy(() => sequencer?.setDealer(null));
  }
}
