import { Component, computed, input } from '@angular/core';

/** Code de table du design system : un caractère par case, en or. */
@Component({
  selector: 'app-room-code',
  template: `
    @for (char of chars(); track $index) {
      <span class="char" aria-hidden="true">{{ char }}</span>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      gap: var(--space-2);
      --w: 56px;
    }

    :host(.is-small) {
      --w: 32px;
      gap: var(--space-1);
    }

    :host(.is-large) {
      --w: 72px;
    }

    .char {
      display: grid;
      place-items: center;
      width: var(--w);
      height: calc(var(--w) * 1.3);
      border: 1px solid var(--gold);
      border-radius: var(--radius-md);
      background: var(--surface-200);
      color: var(--gold);
      font: 700 calc(var(--w) * 0.7) / 1 var(--font-display);
    }
  `,
  host: {
    role: 'group',
    '[class.is-small]': 'size() === "sm"',
    '[class.is-large]': 'size() === "lg"',
    '[attr.aria-label]': '"Code de la table : " + chars().join(", ")',
  },
})
export class RoomCode {
  readonly code = input.required<string>();
  readonly size = input<'sm' | 'md' | 'lg'>('md');

  protected readonly chars = computed(() => [...this.code()]);
}
