import { Component, effect, input, signal } from '@angular/core';

/** QR code d'un texte (lien d'invitation). La librairie n'est chargée qu'à l'affichage. */
@Component({
  selector: 'app-qr-code',
  template: `
    @if (dataUrl(); as src) {
      <img [src]="src" [alt]="alt()" />
    }
  `,
  styles: `
    :host {
      display: block;
      width: var(--qr-size, 9rem);
      aspect-ratio: 1;
      border-radius: var(--radius-sm);
      background: #fff;
      overflow: hidden;
    }

    img {
      display: block;
      width: 100%;
      height: 100%;
    }
  `,
})
export class QrCode {
  readonly value = input.required<string>();
  readonly alt = input('QR code');

  protected readonly dataUrl = signal<string | null>(null);

  constructor() {
    effect((onCleanup) => {
      const value = this.value();
      let cancelled = false;
      onCleanup(() => (cancelled = true));

      void import('qrcode')
        .then((qrcode) => qrcode.toDataURL(value, { margin: 1, width: 320 }))
        .then((url) => {
          if (!cancelled) {
            this.dataUrl.set(url);
          }
        })
        .catch(() => this.dataUrl.set(null));
    });
  }
}
