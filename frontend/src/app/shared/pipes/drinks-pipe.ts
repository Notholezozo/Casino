import { Pipe, PipeTransform } from '@angular/core';

type DrinkUnit = 'Sip' | 'CulSec';

/** « 1 gorgée », « 3 gorgées », « 1 cul sec », « 2 culs secs ». */
export function formatDrinks(amount: number, unit: DrinkUnit = 'Sip'): string {
  const plural = amount > 1;
  const noun = unit === 'CulSec' ? (plural ? 'culs secs' : 'cul sec') : plural ? 'gorgées' : 'gorgée';
  return `${amount} ${noun}`;
}

/**
 * Ce qu'un joueur doit boire, les deux unités réunies : « 3 gorgées et 1 cul sec ».
 * Renvoie null s'il n'y a rien à boire.
 */
export function formatToDrink(sips: number, culsSecs: number): string | null {
  const parts = [
    culsSecs > 0 ? formatDrinks(culsSecs, 'CulSec') : null,
    sips > 0 ? formatDrinks(sips, 'Sip') : null,
  ].filter((part) => part !== null);
  return parts.length > 0 ? parts.join(' et ') : null;
}

/**
 * Cumul compact pour une ligne de score : « 11 », ou « 11 + 2 culs secs » s'il y en a.
 * Le nombre seul compte des gorgées.
 */
export function formatTally(sips: number, culsSecs: number): string {
  return culsSecs > 0 ? `${sips} + ${formatDrinks(culsSecs, 'CulSec')}` : `${sips}`;
}

/** Formate une quantité dans son unité : `3 | drinks` → « 3 gorgées », `1 | drinks: 'CulSec'` → « 1 cul sec ». */
@Pipe({ name: 'drinks' })
export class DrinksPipe implements PipeTransform {
  transform(amount: number, unit: DrinkUnit = 'Sip'): string {
    return formatDrinks(amount, unit);
  }
}
