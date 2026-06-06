import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { CookingStatsClient } from '../../api/cooking-stats.client';
import { getErrorMessage } from '../../shared/get-error-message';

@Component({
  selector: 'app-cooking-stats',
  imports: [RouterLink, TranslateModule],
  templateUrl: './cooking-stats.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CookingStats {
  private readonly client = inject(CookingStatsClient);
  private readonly translate = inject(TranslateService);

  protected readonly stats = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly isEmpty = computed(() => {
    const v = this.stats.value();
    return v !== undefined && v.length === 0;
  });

  protected readonly errorMessage = computed(() => {
    const err = this.stats.error();
    return err ? getErrorMessage(err, this.translate) : '';
  });
}
