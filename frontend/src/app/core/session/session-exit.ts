import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Notice } from '../notifications/notice';
import { SessionStore } from './session-store';

/** Point unique pour sortir d'une table : oublie la session et renvoie à l'accueil. */
@Injectable({ providedIn: 'root' })
export class SessionExit {
  private readonly sessions = inject(SessionStore);
  private readonly notice = inject(Notice);
  private readonly router = inject(Router);

  toHome(message?: string): void {
    this.sessions.clear();
    if (message) {
      this.notice.show(message);
    }
    void this.router.navigate(['/']);
  }
}
