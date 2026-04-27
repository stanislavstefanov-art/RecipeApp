import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { MealPlansClient } from '../../api/meal-plans.client';
import { getErrorMessage } from '../../shared/get-error-message';

@Component({
  selector: 'app-meal-plans-list',
  imports: [RouterLink, TranslateModule],
  templateUrl: './meal-plans-list.html',
  styleUrl: './meal-plans-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlansList {
  private readonly client = inject(MealPlansClient);
  private readonly translate = inject(TranslateService);

  protected readonly mealPlans = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly errorMessage = computed(() => {
    const err = this.mealPlans.error();
    return err ? getErrorMessage(err, this.translate) : '';
  });

  protected readonly isEmpty = computed(() => {
    const value = this.mealPlans.value();
    return value !== undefined && value.length === 0;
  });
}
