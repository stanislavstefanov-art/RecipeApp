import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { MealPlansClient } from '../../api/meal-plans.client';

@Component({
  selector: 'app-meal-plans-list',
  imports: [RouterLink],
  templateUrl: './meal-plans-list.html',
  styleUrl: './meal-plans-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlansList {
  private readonly client = inject(MealPlansClient);

  protected readonly mealPlans = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly errorMessage = computed(() => {
    const err = this.mealPlans.error();
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
    const value = this.mealPlans.value();
    return value !== undefined && value.length === 0;
  });
}
