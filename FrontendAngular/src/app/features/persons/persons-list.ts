import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { PersonsClient } from '../../api/persons.client';

export const DIETARY_LABELS: Readonly<Record<number, string>> = {
  1: 'Vegetarian',
  2: 'Pescatarian',
  3: 'Vegan',
  4: 'High protein',
};

export const CONCERN_LABELS: Readonly<Record<number, string>> = {
  1: 'Diabetes',
  2: 'High blood pressure',
  3: 'Gluten intolerance',
};

@Component({
  selector: 'app-persons-list',
  imports: [RouterLink],
  templateUrl: './persons-list.html',
  styleUrl: './persons-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonsList {
  private readonly client = inject(PersonsClient);

  protected readonly persons = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly errorMessage = computed(() => {
    const err = this.persons.error();
    if (!err) {
      return '';
    }
    if (err instanceof HttpErrorResponse) {
      const problem = err.error as { title?: string; detail?: string } | null;
      return problem?.detail ?? problem?.title ?? err.message;
    }
    return err instanceof Error ? err.message : String(err);
  });

  protected readonly isEmpty = computed(() => {
    const value = this.persons.value();
    return value !== undefined && value.length === 0;
  });

  protected readonly dietaryLabels = DIETARY_LABELS;
  protected readonly concernLabels = CONCERN_LABELS;
}
