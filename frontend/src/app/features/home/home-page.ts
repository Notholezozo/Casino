import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { describeApiError } from '../../core/api/api-error';
import { TablesApi } from '../../core/api/tables-api';
import { Notice } from '../../core/notifications/notice';
import { SessionStore } from '../../core/session/session-store';

const NAME_STORAGE_KEY = 'casino.playerName';
const NAME_MAX_LENGTH = 20;
const CODE_MAX_LENGTH = 8;

function readStoredName(): string {
  try {
    return localStorage.getItem(NAME_STORAGE_KEY) ?? '';
  } catch {
    return '';
  }
}

function storeName(name: string): void {
  try {
    localStorage.setItem(NAME_STORAGE_KEY, name);
  } catch {
    // Stockage indisponible : le pseudo ne sera simplement pas mémorisé.
  }
}

/** Accueil : rejoindre une table en invité (code + pseudo) ou en créer une. */
@Component({
  selector: 'app-home-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
})
export class HomePage {
  private readonly tablesApi = inject(TablesApi);
  private readonly sessions = inject(SessionStore);
  private readonly router = inject(Router);
  protected readonly notice = inject(Notice);

  protected readonly nameMaxLength = NAME_MAX_LENGTH;
  protected readonly codeMaxLength = CODE_MAX_LENGTH;

  protected readonly form = inject(FormBuilder).nonNullable.group({
    code: ['', [Validators.required]],
    name: [readStoredName(), [Validators.required, Validators.maxLength(NAME_MAX_LENGTH)]],
  });

  /** Requête en cours, pour désactiver les boutons et afficher le bon libellé. */
  protected readonly pending = signal<'join' | 'create' | null>(null);
  protected readonly error = signal<string | null>(null);

  /** Partie en cours sur cet appareil, pour y revenir après un rechargement. */
  protected readonly resume = computed(() => {
    const session = this.sessions.session();
    if (!session) {
      return null;
    }
    return session.role === 'admin'
      ? { link: ['/table', session.code], text: `Tu gères la table ${session.code}.` }
      : { link: ['/play', session.code], text: `Tu es à la table ${session.code} (${session.name}).` };
  });

  constructor() {
    const code = inject(ActivatedRoute).snapshot.queryParamMap.get('code');
    if (code) {
      this.form.controls.code.setValue(code.trim().toUpperCase().slice(0, CODE_MAX_LENGTH));
    }
  }

  protected join(): void {
    const code = this.form.controls.code.value.trim().toUpperCase();
    const name = this.form.controls.name.value.trim();
    if (!code || !name) {
      this.error.set('Saisis le code de la table et ton pseudo.');
      return;
    }

    this.start('join');
    this.tablesApi
      .join(code, { name })
      .pipe(finalize(() => this.pending.set(null)))
      .subscribe({
        next: (joined) => {
          storeName(joined.name);
          this.sessions.set({
            role: 'player',
            code: joined.code,
            token: joined.token,
            playerId: joined.playerId,
            name: joined.name,
          });
          void this.router.navigate(['/play', joined.code]);
        },
        error: (error: unknown) => this.error.set(describeApiError(error)),
      });
  }

  protected createTable(): void {
    this.start('create');
    this.tablesApi
      .create()
      .pipe(finalize(() => this.pending.set(null)))
      .subscribe({
        next: (table) => {
          this.sessions.set({ role: 'admin', code: table.code, token: table.adminToken });
          void this.router.navigate(['/table', table.code]);
        },
        error: (error: unknown) => this.error.set(describeApiError(error)),
      });
  }

  private start(request: 'join' | 'create'): void {
    this.pending.set(request);
    this.error.set(null);
    this.notice.dismiss();
  }
}
