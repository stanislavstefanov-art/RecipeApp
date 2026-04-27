import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { PersonsClient } from '../../api/persons.client';
import { getErrorMessage } from '../../shared/get-error-message';

@Component({
  selector: 'app-persons-details',
  imports: [RouterLink, TranslateModule],
  templateUrl: './persons-details.html',
  styleUrl: './persons-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonsDetails {
  private readonly client = inject(PersonsClient);
  private readonly translate = inject(TranslateService);

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
    return getErrorMessage(err, this.translate);
  });
}
