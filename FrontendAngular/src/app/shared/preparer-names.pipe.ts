import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'preparerNames', standalone: true, pure: true })
export class PreparerNamesPipe implements PipeTransform {
  transform(preparers: readonly { personName: string }[]): string {
    return preparers.map(p => p.personName).join(', ');
  }
}
