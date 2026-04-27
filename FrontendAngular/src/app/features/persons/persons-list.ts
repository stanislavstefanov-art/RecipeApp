import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { PersonsClient } from '../../api/persons.client';
import { getErrorMessage } from '../../shared/get-error-message';

@Component({
  selector: 'app-persons-list',
  imports: [RouterLink, TranslateModule],
  templateUrl: './persons-list.html',
  styleUrl: './persons-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonsList {
  private readonly client = inject(PersonsClient);
  private readonly translate = inject(TranslateService);

  protected readonly persons = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly errorMessage = computed(() => {
    const err = this.persons.error();
    return err ? getErrorMessage(err, this.translate) : '';
  });

  protected readonly isEmpty = computed(() => {
    const value = this.persons.value();
    return value !== undefined && value.length === 0;
  });
}
