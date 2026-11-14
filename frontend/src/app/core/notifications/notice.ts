import { Injectable, signal } from '@angular/core';

/** Message d'information qui survit à une navigation (ex. « La table a été fermée »). */
@Injectable({ providedIn: 'root' })
export class Notice {
  private readonly current = signal<string | null>(null);

  readonly message = this.current.asReadonly();

  show(message: string): void {
    this.current.set(message);
  }

  dismiss(): void {
    this.current.set(null);
  }
}
