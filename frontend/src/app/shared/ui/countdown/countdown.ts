import { Component, DestroyRef, computed, effect, inject, input, signal } from '@angular/core';

const TICK_MS = 200;
const URGENT_SECONDS = 5;

/**
 * Compte à rebours circulaire vers une échéance locale (epoch ms).
 * La durée totale est déduite du temps restant au moment où l'échéance change.
 */
@Component({
  selector: 'app-countdown',
  templateUrl: './countdown.html',
  styleUrl: './countdown.scss',
  host: {
    role: 'timer',
    '[class.is-urgent]': 'seconds() <= urgentSeconds',
    '[attr.aria-label]': '"Temps restant : " + seconds() + " secondes"',
  },
})
export class Countdown {
  readonly deadline = input.required<number>();

  protected readonly urgentSeconds = URGENT_SECONDS;

  private readonly now = signal(Date.now());
  private readonly totalMs = signal(1);

  private readonly remainingMs = computed(() => Math.max(this.deadline() - this.now(), 0));

  protected readonly seconds = computed(() => Math.ceil(this.remainingMs() / 1000));

  /** Part restante, de 100 (plein) à 0 (échu). */
  protected readonly progress = computed(() =>
    Math.min((this.remainingMs() / this.totalMs()) * 100, 100),
  );

  constructor() {
    effect(() => {
      const now = Date.now();
      this.now.set(now);
      this.totalMs.set(Math.max(this.deadline() - now, 1));
    });

    const timer = setInterval(() => this.now.set(Date.now()), TICK_MS);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }
}
