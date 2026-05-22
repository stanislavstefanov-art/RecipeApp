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
import { Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';

import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { ToastService } from '../../core/toast.service';
import { HouseholdsClient } from '../../api/households.client';
import { MealPlansClient } from '../../api/meal-plans.client';
import { MealPlanEntryAssignmentDto } from '../../api/meal-plans.dto';
import { RecipesClient } from '../../api/recipes.client';
import { getErrorMessage } from '../../shared/get-error-message';

type FormState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'busy' }
  | { readonly kind: 'error'; readonly message: string };

type EditSubmitState = FormState;

type DeleteState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'deleting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-meal-plans-details',
  imports: [RouterLink, ReactiveFormsModule, TranslateModule],
  templateUrl: './meal-plans-details.html',
  styleUrl: './meal-plans-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlansDetails {
  private readonly client = inject(MealPlansClient);
  private readonly recipesClient = inject(RecipesClient);
  private readonly householdsClient = inject(HouseholdsClient);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

  readonly id = input.required<string>();

  protected readonly mealPlan = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.client.get(params),
  });

  protected readonly allRecipes = rxResource({
    stream: () => this.recipesClient.list(),
  });

  protected readonly household = rxResource({
    params: () => this.mealPlan.value()?.householdId,
    stream: ({ params }) => (params ? this.householdsClient.get(params) : of(null)),
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
    return getErrorMessage(err, this.translate);
  });

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

  protected readonly showAddForm = signal(false);
  protected readonly addEntryState = signal<FormState>({ kind: 'idle' });
  protected readonly addForm = new FormGroup({
    recipeId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    plannedDate: new FormControl(new Date().toISOString().slice(0, 10), {
      nonNullable: true,
      validators: [Validators.required],
    }),
    mealType: new FormControl('3', { nonNullable: true, validators: [Validators.required] }),
  });

  protected onToggleAddForm(): void {
    this.showAddForm.update((v) => !v);
    this.addEntryState.set({ kind: 'idle' });
    this.addForm.reset({
      recipeId: '',
      plannedDate: new Date().toISOString().slice(0, 10),
      mealType: '3',
    });
  }

  protected onSubmitAddEntry(): void {
    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }

    const members = this.household.value()?.members ?? [];
    const { recipeId, plannedDate, mealType } = this.addForm.getRawValue();

    this.addEntryState.set({ kind: 'busy' });
    this.client
      .addEntry(this.id(), {
        recipeId,
        plannedDate,
        mealType: parseInt(mealType, 10),
        scope: 1,
        assignments: members.map((m) => ({
          personId: m.personId,
          assignedRecipeId: recipeId,
          recipeVariationId: null,
          portionMultiplier: 1,
          notes: null,
        })),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.addEntryState.set({ kind: 'idle' });
          this.showAddForm.set(false);
          this.mealPlan.reload();
        },
        error: (err: unknown) => {
          this.addEntryState.set({
            kind: 'error',
            message: getErrorMessage(err, this.translate, 'Failed to add entry'),
          });
        },
      });
  }

  private readonly deleteState = signal<DeleteState>({ kind: 'idle' });

  protected readonly isDeleting = computed(() => this.deleteState().kind === 'deleting');
  protected readonly deleteError = computed(() => {
    const s = this.deleteState();
    return s.kind === 'error' ? s.message : '';
  });

  protected onDelete(): void {
    const confirmed = window.confirm(this.translate.instant('mealPlans.confirmDelete'));
    if (!confirmed) return;

    this.deleteState.set({ kind: 'deleting' });
    this.client
      .delete(this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => void this.router.navigate(['/meal-plans']),
        error: (err: unknown) => {
          this.deleteState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed to delete') });
        },
      });
  }

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
          this.toast.show('success', this.translate.instant('mealPlans.assignmentUpdated', { name: ea.assignment.personName }));
        },
        error: (err: unknown) => {
          this.editSubmitState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed to update') });
        },
      });
  }
}
