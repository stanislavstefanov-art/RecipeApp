import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { HouseholdsClient } from '../../api/households.client';
import { getErrorMessage } from '../../shared/get-error-message';

@Component({
  selector: 'app-households-list',
  imports: [RouterLink, TranslateModule],
  templateUrl: './households-list.html',
  styleUrl: './households-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HouseholdsList {
  private readonly client = inject(HouseholdsClient);
  private readonly translate = inject(TranslateService);

  protected readonly households = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly errorMessage = computed(() => {
    const err = this.households.error();
    return err ? getErrorMessage(err, this.translate) : '';
  });

  protected readonly isEmpty = computed(() => {
    const value = this.households.value();
    return value !== undefined && value.length === 0;
  });
}
