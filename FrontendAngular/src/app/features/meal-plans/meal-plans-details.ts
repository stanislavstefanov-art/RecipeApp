import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { of } from 'rxjs';

import { ToastService } from '../../core/toast.service';
import { MealPlansClient } from '../../api/meal-plans.client';
import { MealPlanEntryAssignmentDto } from '../../api/meal-plans.dto';
import { RecipesClient } from '../../api/recipes.client';

type EditSubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'busy' }
  | { readonly kind: 'error'; readonly message: string };

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
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './meal-plans-details.html',
  styleUrl: './meal-plans-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlansDetails {
  private readonly client = inject(MealPlansClient);
  private readonly recipesClient = inject(RecipesClient);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly id = input.required<string>();

  protected readonly mealPlan = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.client.get(params),
  });

  protected readonly allRecipes = rxResource({
    stream: () => this.recipesClient.list(),
  });

  protected readonly editRecipeId = signal('');

  protected readonly recipeForEdit = rxResource({
    params: () => this.editRecipeId(),
    stream: ({ params }) => (params ? this.recipesClient.get(params) : of(null)),
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

  protected readonly editingAssignment = signal<{
    readonly mealPlanEntryId: string;
    readonly assignment: MealPlanEntryAssignmentDto;
  } | null>(null);

  protected readonly editForm = new FormGroup({
    assignedRecipeId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    recipeVariationId: new FormControl('', { nonNullable: true }),
    portionMultiplier: new FormControl('1', {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0.01)],
    }),
    notes: new FormControl('', { nonNullable: true }),
  });

  protected readonly editSubmitState = signal<EditSubmitState>({ kind: 'idle' });

  protected isEditing(entryId: string, personId: string): boolean {
    const ea = this.editingAssignment();
    return ea !== null && ea.mealPlanEntryId === entryId && ea.assignment.personId === personId;
  }

  protected onOpenEdit(entryId: string, assignment: MealPlanEntryAssignmentDto): void {
    this.editingAssignment.set({ mealPlanEntryId: entryId, assignment });
    this.editRecipeId.set(assignment.assignedRecipeId);
    this.editForm.setValue({
      assignedRecipeId: assignment.assignedRecipeId,
      recipeVariationId: assignment.recipeVariationId ?? '',
      portionMultiplier: String(assignment.portionMultiplier),
      notes: assignment.notes ?? '',
    });
    this.editSubmitState.set({ kind: 'idle' });
  }

  protected onCancelEdit(): void {
    this.editingAssignment.set(null);
  }

  protected onEditRecipeChange(recipeId: string): void {
    this.editRecipeId.set(recipeId);
    this.editForm.controls.recipeVariationId.setValue('');
  }

  protected onSubmitEdit(): void {
    const ea = this.editingAssignment();
    if (!ea || this.editForm.invalid) return;

    const { assignedRecipeId, recipeVariationId, portionMultiplier, notes } =
      this.editForm.getRawValue();
    this.editSubmitState.set({ kind: 'busy' });

    this.client
      .updateAssignment(this.id(), ea.mealPlanEntryId, {
        personId: ea.assignment.personId,
        assignedRecipeId,
        recipeVariationId: recipeVariationId || null,
        portionMultiplier: parseFloat(portionMultiplier),
        notes: notes || null,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.editingAssignment.set(null);
          this.editSubmitState.set({ kind: 'idle' });
          this.mealPlan.reload();
          this.toast.show('success', `Assignment updated for ${ea.assignment.personName}`);
        },
        error: (err: Error) => {
          this.editSubmitState.set({ kind: 'error', message: err.message ?? 'Failed to update' });
        },
      });
  }
}
