import { Component, computed, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  DrinkAllocation,
  PlaceBetRequest,
  PlayerAction,
  PlayerView,
} from '../../../core/models/blackjack.models';
import { SessionStore } from '../../../core/session/session-store';
import { formatToDrink } from '../../../shared/pipes/drinks-pipe';
import { Countdown } from '../../../shared/ui/countdown/countdown';
import { DealerMood } from '../../../shared/ui/dealer-3d/dealer-3d';
import { DealSequencer } from '../../../shared/ui/dealing/deal-sequencer';
import { ActionBar } from '../components/action-bar/action-bar';
import { BetPicker } from '../components/bet-picker/bet-picker';
import { DealerAvatar } from '../../table-layout/dealer-avatar';
import { PlayerShell } from '../../table-layout/player-shell';
import { DealerHand } from '../components/dealer-hand/dealer-hand';
import { DistributionForm } from '../components/distribution-form/distribution-form';
import { HandView } from '../components/hand/hand';
import { RoundResultView } from '../components/round-result/round-result';
import { PlayerStore } from '../state/player-store';

/** Ce que le joueur doit voir en bas de l'écran, déduit de la vue serveur. */
type PlayerMode =
  | 'betting'
  | 'spectating'
  | 'my-turn'
  | 'waiting-turn'
  | 'dealer'
  | 'distributing'
  | 'distribution-wait'
  | 'results';

function resolveMode(view: PlayerView): PlayerMode {
  const { me, phase } = view;
  switch (phase) {
    case 'Betting':
      return 'betting';
    case 'PlayerTurns':
      if (!me.inRound) {
        return 'spectating';
      }
      return me.isMyTurn ? 'my-turn' : 'waiting-turn';
    case 'DealerTurn':
      return me.inRound ? 'dealer' : 'spectating';
    case 'Distribution': {
      const result = me.result;
      const mustDistribute =
        !!result && result.toDistribute > 0 && result.canDistribute && !result.distributionDone;
      return mustDistribute ? 'distributing' : 'distribution-wait';
    }
    case 'Results':
      return 'results';
  }
}

/** Le croupier jubile quand le joueur perd, et fait la tête quand il gagne. */
function resolveDealerMood(view: PlayerView): DealerMood {
  const result = view.me.result;
  if (!result || (view.phase !== 'Distribution' && view.phase !== 'Results')) {
    return 'idle';
  }
  if (result.toDistribute > 0 && result.lost === 0) {
    return 'sad';
  }
  return result.lost > 0 ? 'happy' : 'idle';
}

/** Vue joueur (téléphone) : ses cartes, le dealer et ses options — rien des autres mains. */
@Component({
  selector: 'bj-player-page',
  imports: [
    ActionBar,
    BetPicker,
    Countdown,
    DealerAvatar,
    DealerHand,
    DistributionForm,
    HandView,
    PlayerShell,
    RoundResultView,
    RouterLink,
  ],
  providers: [PlayerStore, DealSequencer],
  templateUrl: './player-page.html',
  styleUrl: './player-page.scss',
})
export class PlayerPage {
  protected readonly store = inject(PlayerStore);
  protected readonly view = this.store.view;

  protected readonly mode = computed(() => {
    const view = this.view();
    return view ? resolveMode(view) : null;
  });

  protected readonly dealerMood = computed<DealerMood>(() => {
    const view = this.view();
    return view ? resolveDealerMood(view) : 'idle';
  });

  /** Rappel de la manche précédente, affiché pendant qu'on mise la suivante. */
  protected readonly lastToDrink = computed(() => {
    const last = this.view()?.me.result;
    return last ? (formatToDrink(last.sipsToDrink, last.culsSecsToDrink) ?? 'rien à boire') : null;
  });

  protected readonly statusText = computed(() => {
    const view = this.view();
    if (!view) {
      return '';
    }
    switch (resolveMode(view)) {
      case 'betting':
        return view.me.bet === null ? 'Place ta mise' : 'En attente des autres joueurs…';
      case 'spectating':
        return 'Manche en cours : tu joues la prochaine.';
      case 'my-turn':
        return 'À toi de jouer';
      case 'waiting-turn': {
        const active = view.others.find((player) => player.id === view.activePlayerId);
        return active ? `Au tour de ${active.name}` : 'En attente…';
      }
      case 'dealer':
        return 'Le dealer joue…';
      case 'distributing':
        return 'Tu as gagné : distribue';
      case 'distribution-wait':
        return 'Les gagnants distribuent…';
      case 'results':
        return 'Fin de la manche';
    }
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

  protected placeBet(bet: PlaceBetRequest): void {
    this.store.placeBet(bet);
  }

  protected act(action: PlayerAction): void {
    this.store.act(action);
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
