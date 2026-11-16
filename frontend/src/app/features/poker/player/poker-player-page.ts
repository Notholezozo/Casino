import { Component, computed, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DrinkAllocation, OtherPlayer } from '../../../core/models/blackjack.models';
import { PokerActionRequest, PokerPlayerView } from '../../../core/models/poker.models';
import { SessionStore } from '../../../core/session/session-store';
import { DrinksPipe } from '../../../shared/pipes/drinks-pipe';
import { Countdown } from '../../../shared/ui/countdown/countdown';
import { DealSequencer } from '../../../shared/ui/dealing/deal-sequencer';
import { PlayingCard } from '../../../shared/ui/playing-card/playing-card';
import { DistributionForm } from '../../blackjack/components/distribution-form/distribution-form';
import { DealerAvatar } from '../../table-layout/dealer-avatar';
import { PlayerShell } from '../../table-layout/player-shell';
import { Board } from '../components/board';
import { PokerActionBar } from '../components/poker-action-bar';
import { describePokerPhase, isBettingPhase, seatRole } from '../poker-labels';
import { PokerPlayerStore } from '../state/poker-stores';

/** Ce que le joueur doit voir en bas de l'écran, déduit de la vue serveur. */
type PokerMode = 'waiting' | 'sitting-out' | 'my-turn' | 'waiting-turn' | 'distributing' | 'summary';

function resolveMode(view: PokerPlayerView): PokerMode {
  const me = view.seats.find((seat) => seat.playerId === view.me.id);

  if (view.phase === 'Distribution' || view.phase === 'Results') {
    const mustDistribute =
      view.phase === 'Distribution' &&
      !!me &&
      me.toDistribute > 0 &&
      view.me.canDistribute &&
      !me.distributionDone;
    return mustDistribute ? 'distributing' : 'summary';
  }
  if (!isBettingPhase(view.phase)) {
    return 'waiting';
  }
  if (!me?.inHand || me.folded) {
    return 'sitting-out';
  }
  return view.me.isMyTurn ? 'my-turn' : 'waiting-turn';
}

/** Vue joueur du poker (téléphone) : ses deux cartes, les cartes communes et ses options. */
@Component({
  selector: 'pk-player-page',
  imports: [
    Board,
    Countdown,
    DealerAvatar,
    DistributionForm,
    DrinksPipe,
    PlayerShell,
    PlayingCard,
    PokerActionBar,
    RouterLink,
  ],
  providers: [PokerPlayerStore, DealSequencer],
  templateUrl: './poker-player-page.html',
  styleUrl: './poker-player-page.scss',
})
export class PokerPlayerPage {
  protected readonly store = inject(PokerPlayerStore);
  protected readonly view = this.store.view;

  protected readonly mode = computed(() => {
    const view = this.view();
    return view ? resolveMode(view) : null;
  });

  /** La place du joueur lui-même. */
  protected readonly me = computed(() => {
    const view = this.view();
    return view?.seats.find((seat) => seat.playerId === view.me.id) ?? null;
  });

  protected readonly myRole = computed(() => {
    const me = this.me();
    return me ? seatRole(me) : null;
  });

  /** Les autres joueurs : pour suivre la main, et pour leur distribuer le pot. */
  protected readonly others = computed(() => {
    const view = this.view();
    return (view?.seats ?? [])
      .filter((seat) => seat.playerId !== view!.me.id)
      .map((seat) => ({ seat, role: seatRole(seat) }));
  });

  /** Les mêmes, au format attendu par le formulaire de distribution. */
  protected readonly recipients = computed<OtherPlayer[]>(() =>
    this.others().map(({ seat }) => ({
      id: seat.playerId,
      name: seat.name,
      isConnected: seat.isConnected,
      inRound: seat.inHand,
    })),
  );

  protected readonly statusText = computed(() => {
    const view = this.view();
    if (!view) {
      return '';
    }
    if (view.me.isMyTurn) {
      return 'À toi de parler';
    }
    if (this.mode() === 'distributing') {
      return 'Tu as gagné : distribue le pot';
    }
    return describePokerPhase(view);
  });

  constructor() {
    // Le garde de route garantit une session joueur pour ce code.
    const session = inject(SessionStore).session();
    if (session?.role === 'player') {
      this.store.connect(session);
    }

    // Les cartes déjà sur la table à l'arrivée ne sont pas redistribuées : on n'anime
    // qu'à partir du moment où le premier état est affiché.
    const sequencer = inject(DealSequencer);
    effect(() => {
      if (this.view()) {
        setTimeout(() => sequencer.arm());
      }
    });
  }

  protected act(request: PokerActionRequest): void {
    this.store.act(request);
  }

  protected distribute(allocations: DrinkAllocation[]): void {
    this.store.distribute(allocations);
  }

  protected leave(): void {
    if (confirm('Quitter la table ?')) {
      this.store.leave();
    }
  }
}
