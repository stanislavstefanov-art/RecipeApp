import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { HouseholdsClient } from '../../api/households.client';

@Component({
  selector: 'app-households-list',
  imports: [RouterLink],
  templateUrl: './households-list.html',
  styleUrl: './households-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HouseholdsList {
  private readonly client = inject(HouseholdsClient);

  protected readonly households = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly errorMessage = computed(() => {
    const err = this.households.error();
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
    const value = this.households.value();
    return value !== undefined && value.length === 0;
  });
}
