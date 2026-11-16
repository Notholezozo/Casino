import { Routes } from '@angular/router';
import {
  adminSessionGuard,
  playerSessionGuard,
  sessionGameIs,
} from './core/session/session-guards';

// Un jeu = un dossier sous features/. Les routes /play et /table sont communes : chaque jeu
// déclare les siennes avec `canMatch`, et la première qui correspond au jeu de la table gagne.
// Le blackjack, sans condition, sert de repli (et redirige vers l'accueil sans session valide).
export const routes: Routes = [
  {
    path: '',
    title: 'Grand Salon',
    loadComponent: () => import('./features/home/home-page').then((m) => m.HomePage),
  },
  {
    path: 'play/:code',
    title: 'Grand Salon – Ma main',
    canMatch: [sessionGameIs('Poker')],
    canActivate: [playerSessionGuard],
    loadComponent: () =>
      import('./features/poker/player/poker-player-page').then((m) => m.PokerPlayerPage),
  },
  {
    path: 'table/:code',
    title: 'Grand Salon – Table',
    canMatch: [sessionGameIs('Poker')],
    canActivate: [adminSessionGuard],
    loadComponent: () =>
      import('./features/poker/table/poker-table-page').then((m) => m.PokerTablePage),
  },
  {
    path: 'play/:code',
    title: 'Grand Salon – Ma main',
    canActivate: [playerSessionGuard],
    loadComponent: () =>
      import('./features/blackjack/player/player-page').then((m) => m.PlayerPage),
  },
  {
    path: 'table/:code',
    title: 'Grand Salon – Table',
    canActivate: [adminSessionGuard],
    loadComponent: () => import('./features/blackjack/table/table-page').then((m) => m.TablePage),
  },
  { path: '**', redirectTo: '' },
];
