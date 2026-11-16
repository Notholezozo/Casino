import { Component, computed, inject, input, output, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';
import { DrinkTotals, DrinkTransfer } from '../../core/models/blackjack.models';
import { InviteLinkResolver } from '../../core/network/invite-link';
import { DrinksPipe } from '../../shared/pipes/drinks-pipe';
import { Countdown } from '../../shared/ui/countdown/countdown';
import { DealerMood } from '../../shared/ui/dealer-3d/dealer-3d';
import { QrCode } from '../../shared/ui/qr-code/qr-code';
import { RoomCode } from '../../shared/ui/room-code/room-code';
import { DealerAvatar } from './dealer-avatar';
import { RAIL_STUDS, TABLE_VIEWBOX } from './table-geometry';

/** Ce que la mise en page a besoin de savoir d'un joueur pour le tableau des scores. */
export interface ScoredPlayer {
  name: string;
  totals: DrinkTotals;
}

/** Cumul abrégé pour le bandeau, où la place est comptée : « 12 » ou « 12 + 2 CS ». */
function shortTally(sips: number, culsSecs: number): string {
  return culsSecs > 0 ? `${sips} + ${culsSecs} CS` : `${sips}`;
}

/**
 * Mise en page de l'écran table, commune à tous les jeux : le bandeau d'informations,
 * le menu d'invitation, la table en demi-cercle et son croupier.
 *
 * Chaque jeu y projette ce qui lui est propre :
 * - `[tableMeter]` : son indicateur dans le bandeau (sabot, pot…) ;
 * - `[tableDealer]` : ce que le croupier a devant lui (sa main, les cartes communes…) ;
 * - le reste : les places, chacune avec la classe globale `table-seat` et sa position
 *   (`--x`, `--y`, voir table-geometry.ts).
 */
@Component({
  selector: 'app-table-layout',
  imports: [Countdown, DealerAvatar, DrinksPipe, QrCode, RoomCode],
  templateUrl: './table-layout.html',
  styleUrl: './table-layout.scss',
})
export class TableLayout {
  private readonly inviteLinks = inject(InviteLinkResolver);

  /** Nom du jeu, en titre du bandeau. */
  readonly title = input.required<string>();
  /** « Manche 3 », « Main 3 »… */
  readonly roundLabel = input.required<string>();
  readonly phaseText = input.required<string>();
  /** Échéance de la phase en epoch ms locales, ou null sans minuteur. */
  readonly deadline = input<number | null>(null);
  readonly reconnecting = input(false);
  readonly tableCode = input.required<string>();
  readonly players = input.required<readonly ScoredPlayer[]>();
  /** Qui a donné à qui pendant la manche qui vient de se jouer. */
  readonly transfers = input<readonly DrinkTransfer[]>([]);
  /** Vrai tant qu'aucune manche n'a été jouée : le menu d'invitation reste alors ouvert. */
  readonly lobby = input(false);
  readonly busy = input(false);
  readonly dealerMood = input<DealerMood>('idle');
  /** Inscriptions en arc sur le tapis. */
  readonly feltTitle = input.required<string>();
  readonly feltRule = input.required<string>();

  readonly closeTable = output<void>();

  // Dessin de la table : voir table-geometry.ts.
  protected readonly viewBox = `0 0 ${TABLE_VIEWBOX.width} ${TABLE_VIEWBOX.height}`;
  protected readonly studs = RAIL_STUDS;

  /** Lien d'invitation joignable depuis les téléphones ; null le temps de le résoudre. */
  protected readonly invite = toSignal(
    toObservable(this.tableCode).pipe(switchMap((code) => this.inviteLinks.resolve(code))),
    { initialValue: null },
  );

  /** Choix explicite de l'hôte pour le menu d'invitation ; null tant qu'il n'y a pas touché. */
  private readonly inviteChoice = signal<boolean | null>(null);

  /**
   * Menu d'invitation (code + QR) ouvert ou non. Sans choix de l'hôte, il reste ouvert tant
   * qu'aucune manche n'a été jouée, puis se referme pour laisser la place à la table.
   */
  protected readonly inviteOpen = computed(() => this.inviteChoice() ?? this.lobby());

  /** Cumuls par joueur au format court du bandeau : « 12 », ou « 12 + 2 CS ». */
  protected readonly scores = computed(() =>
    this.players().map(({ name, totals }) => ({
      name,
      drunk: shortTally(totals.sipsDrunk, totals.culsSecsDrunk),
      given: shortTally(totals.sipsGiven, totals.culsSecsGiven),
    })),
  );

  /** Vrai si un joueur a bu ou donné un cul sec : le bandeau affiche alors la légende « CS ». */
  protected readonly hasCulsSecs = computed(() =>
    this.players().some(({ totals }) => totals.culsSecsDrunk > 0 || totals.culsSecsGiven > 0),
  );

  protected toggleInvite(): void {
    this.inviteChoice.set(!this.inviteOpen());
  }
}
