import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { MealPlansClient } from '../../api/meal-plans.client';

export const MEAL_TYPE_LABELS: Readonly<Record<number, string>> = {
  1: 'Breakfast',
  2: 'Lunch',
  3: 'Dinner',
  4: 'Snack',
};

export const MEAL_SCOPE_LABELS: Readonly<Record<number, string>> = {
  1: 'Shared',
  2: 'Shared with variations',
  3: 'Individual',
};

@Component({
  selector: 'app-meal-plans-details',
  imports: [RouterLink],
  templateUrl: './meal-plans-details.html',
  styleUrl: './meal-plans-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlansDetails {
  private readonly client = inject(MealPlansClient);

  readonly id = input.required<string>();

  protected readonly mealPlan = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.client.get(params),
  });

  protected readonly is404 = computed(() => {
    const err = this.mealPlan.error();
    return err instanceof HttpErrorResponse && err.status === 404;
  });

  protected readonly errorMessage = computed(() => {
    const err = this.mealPlan.error();
    if (!err || this.is404()) {
      return '';
    }
    if (err instanceof HttpErrorResponse) {
      const problem = err.error as { title?: string; detail?: string } | null;
      return problem?.detail ?? problem?.title ?? err.message;
    }
    return err instanceof Error ? err.message : String(err);
  });

  protected readonly mealTypeLabels = MEAL_TYPE_LABELS;
  protected readonly mealScopeLabels = MEAL_SCOPE_LABELS;
}
