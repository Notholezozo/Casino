import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlayerAction, PlayerView, SipAllocation } from '../../../core/models/blackjack.models';
import { SessionStore } from '../../../core/session/session-store';
import { SipsPipe } from '../../../shared/pipes/sips-pipe';
import { Countdown } from '../../../shared/ui/countdown/countdown';
import { ActionBar } from '../components/action-bar/action-bar';
import { BetPicker } from '../components/bet-picker/bet-picker';
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
        !!result &&
        result.sipsToDistribute > 0 &&
        result.canDistribute &&
        !result.distributionDone;
      return mustDistribute ? 'distributing' : 'distribution-wait';
    }
    case 'Results':
      return 'results';
  }
}

/** Vue joueur (téléphone) : ses cartes, le croupier et ses options — rien des autres mains. */
@Component({
  selector: 'bj-player-page',
  imports: [
    ActionBar,
    BetPicker,
    Countdown,
    DealerHand,
    DistributionForm,
    HandView,
    RoundResultView,
    RouterLink,
    SipsPipe,
  ],
  providers: [PlayerStore],
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
        return 'À toi de jouer !';
      case 'waiting-turn': {
        const active = view.others.find((player) => player.id === view.activePlayerId);
        return active ? `C'est au tour de ${active.name}` : 'En attente…';
      }
      case 'dealer':
        return 'Le croupier joue…';
      case 'distributing':
        return 'Tu as gagné : distribue tes gorgées !';
      case 'distribution-wait':
        return 'Les gagnants distribuent leurs gorgées…';
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
  }

  protected placeBet(sips: number): void {
    this.store.placeBet(sips);
  }

  protected act(action: PlayerAction): void {
    this.store.act(action);
  }

  protected distribute(allocations: SipAllocation[]): void {
    this.store.distribute(allocations);
  }

  protected leave(): void {
    if (confirm('Quitter la table ?')) {
      this.store.leave();
    }
  }
}
