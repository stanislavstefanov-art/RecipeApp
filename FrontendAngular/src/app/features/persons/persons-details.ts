import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { PersonsClient } from '../../api/persons.client';
import { CONCERN_LABELS, DIETARY_LABELS } from './persons-list';

@Component({
  selector: 'app-persons-details',
  imports: [RouterLink],
  templateUrl: './persons-details.html',
  styleUrl: './persons-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonsDetails {
  private readonly client = inject(PersonsClient);

  readonly id = input.required<string>();

  protected readonly person = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.client.get(params),
  });

  protected readonly is404 = computed(() => {
    const err = this.person.error();
    return err instanceof HttpErrorResponse && err.status === 404;
  });

  protected readonly errorMessage = computed(() => {
    const err = this.person.error();
    if (!err || this.is404()) {
      return '';
    }
    if (err instanceof HttpErrorResponse) {
      const problem = err.error as { title?: string; detail?: string } | null;
      return problem?.detail ?? problem?.title ?? err.message;
    }
    return err instanceof Error ? err.message : String(err);
  });

  protected readonly dietaryLabels = DIETARY_LABELS;
  protected readonly concernLabels = CONCERN_LABELS;
}
