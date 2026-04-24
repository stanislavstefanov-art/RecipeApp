import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, rxResource } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { HouseholdsClient } from '../../api/households.client';
import { MealPlanSuggestionDto } from '../../api/meal-plans.dto';
import { MealPlansClient } from '../../api/meal-plans.client';
import { RecipesClient } from '../../api/recipes.client';
import { MEAL_SCOPE_LABELS, MEAL_TYPE_LABELS } from './meal-plans-details';

type SuggestState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'loading' }
  | { readonly kind: 'error'; readonly message: string }
  | { readonly kind: 'success'; readonly result: MealPlanSuggestionDto };

type AcceptState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-meal-plans-suggest',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './meal-plans-suggest.html',
  styleUrl: './meal-plans-suggest.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlansSuggest {
  private readonly client = inject(MealPlansClient);
  private readonly householdsClient = inject(HouseholdsClient);
  private readonly recipesClient = inject(RecipesClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly households = rxResource({
    stream: () => this.householdsClient.list(),
  });

  protected readonly recipes = rxResource({
    stream: () => this.recipesClient.list(),
  });

  protected readonly recipeMap = computed(
    () => new Map((this.recipes.value() ?? []).map((r) => [r.id, r.name])),
  );

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    householdId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    startDate: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    numberOfDays: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(31)],
    }),
    breakfast: new FormControl(false, { nonNullable: true }),
    lunch: new FormControl(false, { nonNullable: true }),
    dinner: new FormControl(false, { nonNullable: true }),
    snack: new FormControl(false, { nonNullable: true }),
  });

  protected readonly noMealTypeError = signal(false);

  private readonly suggestState = signal<SuggestState>({ kind: 'idle' });
  private readonly acceptState = signal<AcceptState>({ kind: 'idle' });

  protected readonly isSuggesting = computed(() => this.suggestState().kind === 'loading');
  protected readonly suggestError = computed(() => {
    const s = this.suggestState();
    return s.kind === 'error' ? s.message : '';
  });
  protected readonly suggestion = computed(() => {
    const s = this.suggestState();
    return s.kind === 'success' ? s.result : null;
  });

  protected readonly isAccepting = computed(() => this.acceptState().kind === 'submitting');
  protected readonly acceptError = computed(() => {
    const s = this.acceptState();
    return s.kind === 'error' ? s.message : '';
  });

  protected readonly mealTypeLabels = MEAL_TYPE_LABELS;
  protected readonly mealScopeLabels = MEAL_SCOPE_LABELS;

  protected onSuggest(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const mealTypes: number[] = [];
    if (v.breakfast) mealTypes.push(1);
    if (v.lunch) mealTypes.push(2);
    if (v.dinner) mealTypes.push(3);
    if (v.snack) mealTypes.push(4);

    if (mealTypes.length === 0) {
      this.noMealTypeError.set(true);
      return;
    }
    this.noMealTypeError.set(false);

    this.suggestState.set({ kind: 'loading' });

    this.client
      .suggest({
        name: v.name,
        householdId: v.householdId,
        startDate: v.startDate,
        numberOfDays: parseInt(v.numberOfDays, 10),
        mealTypes,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.suggestState.set({ kind: 'success', result });
          this.acceptState.set({ kind: 'idle' });
        },
        error: (err: unknown) => {
          this.suggestState.set({ kind: 'error', message: this.toMessage(err) });
        },
      });
  }

  protected onAccept(): void {
    const s = this.suggestState();
    if (s.kind !== 'success') {
      return;
    }

    const v = this.form.getRawValue();
    const result = s.result;

    this.acceptState.set({ kind: 'submitting' });

    this.client
      .acceptSuggestion({
        name: result.name,
        householdId: v.householdId,
        entries: result.entries.map((e) => ({
          baseRecipeId: e.baseRecipeId,
          plannedDate: e.plannedDate,
          mealType: e.mealType,
          scope: e.scope,
          assignments: e.assignments.map((a) => ({
            personId: a.personId,
            assignedRecipeId: a.assignedRecipeId,
            recipeVariationId: a.recipeVariationId,
            portionMultiplier: a.portionMultiplier,
            notes: a.notes,
          })),
        })),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          void this.router.navigate(['/meal-plans', response.mealPlanId]);
        },
        error: (err: unknown) => {
          this.acceptState.set({ kind: 'error', message: this.toMessage(err) });
        },
      });
  }

  private toMessage(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      const problem = err.error as { title?: string; detail?: string } | null;
      return problem?.detail ?? problem?.title ?? err.message;
    }
    if (err instanceof Error) {
      return err.message;
    }
    return 'Something went wrong.';
  }
}
