import { Routes } from '@angular/router';
import { adminSessionGuard, playerSessionGuard } from './core/session/session-guards';

// Un jeu = un dossier sous features/. Les routes /play et /table pointent aujourd'hui
// sur le blackjack ; elles aiguilleront selon le jeu de la table quand il y en aura d'autres.
export const routes: Routes = [
  {
    path: '',
    title: 'Blackjack',
    loadComponent: () => import('./features/home/home-page').then((m) => m.HomePage),
  },
  {
    path: 'play/:code',
    title: 'Blackjack – Ma main',
    canActivate: [playerSessionGuard],
    loadComponent: () =>
      import('./features/blackjack/player/player-page').then((m) => m.PlayerPage),
  },
  {
    path: 'table/:code',
    title: 'Blackjack – Table',
    canActivate: [adminSessionGuard],
    loadComponent: () => import('./features/blackjack/table/table-page').then((m) => m.TablePage),
  },
  { path: '**', redirectTo: '' },
];
