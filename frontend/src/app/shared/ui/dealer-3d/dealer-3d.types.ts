// Types et constantes du croupier 3D qui ne dépendent pas de three.js :
// le composant peut les importer sans tirer la librairie dans le bundle initial.

export type DealerMood = 'idle' | 'happy' | 'sad';

/** Délai (ms) entre l'appel à dealTo() et l'instant où la carte quitte la main. */
export const DEAL_RELEASE_MS = 150;

export interface ViewportPoint {
  x: number;
  y: number;
}
