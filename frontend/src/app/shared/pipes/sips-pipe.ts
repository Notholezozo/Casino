import { Pipe, PipeTransform } from '@angular/core';

/** Formate une quantité de gorgées : 1 → « 1 gorgée », 3 → « 3 gorgées ». */
@Pipe({ name: 'sips' })
export class SipsPipe implements PipeTransform {
  transform(count: number): string {
    return `${count} ${count > 1 ? 'gorgées' : 'gorgée'}`;
  }
}
