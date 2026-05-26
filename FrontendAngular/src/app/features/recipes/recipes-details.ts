import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { LogCookingValue } from './log-cooking-form';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { RecipesClient } from '../../api/recipes.client';
import { ToastService } from '../../core/toast.service';
import { getErrorMessage } from '../../shared/get-error-message';
import { StarRatingComponent } from '../../shared/ui/star-rating/star-rating';
import { UnitNamePipe } from '../../shared/unit-name.pipe';
import { AddIngredientForm } from './add-ingredient-form';
import { AddStepForm } from './add-step-form';
import { LogCookingFormComponent } from './log-cooking-form';
import { SuggestSubstitutionsForm } from './suggest-substitutions-form';
import { UpdateRecipeNameForm } from './update-recipe-name-form';

type DeleteState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'deleting' }
  | { readonly kind: 'error'; readonly message: string };

type RatingState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'saving' }
  | { readonly kind: 'deleting' };

type ImageState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'uploading' }
  | { readonly kind: 'deleting' };

@Component({
  selector: 'app-recipes-details',
  imports: [RouterLink, FormsModule, ReactiveFormsModule, UpdateRecipeNameForm, AddIngredientForm, AddStepForm, LogCookingFormComponent, SuggestSubstitutionsForm, TranslateModule, StarRatingComponent, UnitNamePipe],
  templateUrl: './recipes-details.html',
  styleUrl: './recipes-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipesDetails {
  private readonly client = inject(RecipesClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);

  readonly id = input.required<string>();

  protected readonly recipe = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.client.get(params),
  });

  private readonly deleteState = signal<DeleteState>({ kind: 'idle' });

  protected readonly isDeleting = computed(
    () => this.deleteState().kind === 'deleting',
  );

  protected readonly deleteError = computed(() => {
    const state = this.deleteState();
    return state.kind === 'error' ? state.message : '';
  });

  protected onNameSaved(): void {
    this.recipe.reload();
  }

  protected onIngredientAdded(): void {
    this.recipe.reload();
  }

  protected onStepAdded(): void {
    this.recipe.reload();
  }

  protected readonly deletingIngredientId = signal<string | null>(null);
  protected readonly deletingStepId = signal<string | null>(null);
  protected readonly editingIngredientId = signal<string | null>(null);
  protected readonly savingIngredientId = signal<string | null>(null);

  protected readonly editIngredientForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
    quantity: new FormControl<number>(0, { nonNullable: true, validators: [Validators.required, Validators.min(0.001)] }),
    unit: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(50)] }),
  });

  protected onEditIngredient(ingredient: { id: string; name: string; quantity: number; unit: string }): void {
    this.editingIngredientId.set(ingredient.id);
    this.editIngredientForm.setValue({
      name: ingredient.name,
      quantity: ingredient.quantity,
      unit: ingredient.unit,
    });
  }

  protected onCancelEditIngredient(): void {
    this.editingIngredientId.set(null);
  }

  protected onSaveEditIngredient(ingredientId: string): void {
    if (this.editIngredientForm.invalid) {
      this.editIngredientForm.markAllAsTouched();
      return;
    }

    const { name, quantity, unit } = this.editIngredientForm.getRawValue();
    this.savingIngredientId.set(ingredientId);
    this.client
      .updateIngredient(this.id(), ingredientId, { name, quantity, unit })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.savingIngredientId.set(null);
          this.editingIngredientId.set(null);
          this.recipe.reload();
        },
        error: () => this.savingIngredientId.set(null),
      });
  }

  protected onDeleteIngredient(ingredientId: string): void {
    if (!window.confirm(this.translate.instant('recipes.confirmDeleteIngredient'))) return;

    this.deletingIngredientId.set(ingredientId);
    this.client
      .removeIngredient(this.id(), ingredientId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deletingIngredientId.set(null);
          this.recipe.reload();
        },
        error: () => this.deletingIngredientId.set(null),
      });
  }

  protected onDeleteStep(stepId: string): void {
    if (!window.confirm(this.translate.instant('recipes.confirmDeleteStep'))) return;

    this.deletingStepId.set(stepId);
    this.client
      .removeStep(this.id(), stepId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deletingStepId.set(null);
          this.recipe.reload();
        },
        error: () => this.deletingStepId.set(null),
      });
  }

  protected onDelete(): void {
    if (!window.confirm(this.translate.instant('recipes.confirmDelete'))) {
      return;
    }

    this.deleteState.set({ kind: 'deleting' });

    this.client
      .delete(this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          void this.router.navigate(['/recipes']);
        },
        error: (err: unknown) => {
          this.deleteState.set({ kind: 'error', message: this.toDeleteMessage(err) });
        },
      });
  }

  private toDeleteMessage(err: unknown): string {
    return getErrorMessage(err, this.translate, 'Failed to delete recipe.');
  }

  protected readonly isNotFound = computed(() => {
    const err = this.recipe.error() as { status?: number } | null;
    return err?.status === 404;
  });

  protected readonly errorMessage = computed(() => {
    const err = this.recipe.error();
    if (!err || this.isNotFound()) {
      return '';
    }
    return getErrorMessage(err, this.translate);
  });

  protected readonly hasIngredients = computed(() => {
    const value = this.recipe.value();
    return value !== undefined && value.ingredients.length > 0;
  });

  protected readonly hasSteps = computed(() => {
    const value = this.recipe.value();
    return value !== undefined && value.steps.length > 0;
  });

  // --- Ratings ---

  private readonly ratingState = signal<RatingState>({ kind: 'idle' });
  protected readonly isSavingRating = computed(() => this.ratingState().kind === 'saving');
  protected readonly isDeletingRating = computed(() => this.ratingState().kind === 'deleting');

  protected selectedStars = signal<number | null>(null);
  protected ratingComment = signal('');

  constructor() {
    effect(() => {
      // rxResource.value() throws when the resource is in error state — guard with hasValue()
      if (!this.recipe.hasValue()) return;
      const myRating = this.recipe.value()?.myRating;
      if (myRating && this.selectedStars() === null) {
        this.selectedStars.set(myRating.stars);
        this.ratingComment.set(myRating.comment ?? '');
      }
    });
  }

  protected onStarsSelected(stars: number): void {
    this.selectedStars.set(stars);
  }

  protected onSaveRating(): void {
    const stars = this.selectedStars();
    if (!stars) return;

    this.ratingState.set({ kind: 'saving' });
    this.client
      .rate(this.id(), { stars, comment: this.ratingComment().trim() || null })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.ratingState.set({ kind: 'idle' });
          this.recipe.reload();
        },
        error: () => this.ratingState.set({ kind: 'idle' }),
      });
  }

  protected onDeleteRating(): void {
    if (!window.confirm(this.translate.instant('ratings.confirmDeleteRating'))) return;

    this.ratingState.set({ kind: 'deleting' });
    this.client
      .deleteRating(this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.ratingState.set({ kind: 'idle' });
          this.selectedStars.set(null);
          this.ratingComment.set('');
          this.recipe.reload();
        },
        error: () => this.ratingState.set({ kind: 'idle' }),
      });
  }

  // --- Image upload ---

  private readonly imageState = signal<ImageState>({ kind: 'idle' });
  protected readonly isUploadingImage = computed(() => this.imageState().kind === 'uploading');
  protected readonly isDeletingImage = computed(() => this.imageState().kind === 'deleting');

  protected onUploadImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    const raw = input.files?.[0];
    if (!raw) return;

    // Android gallery often reports file.type as '' — infer from extension
    const mimeMap: Record<string, string> = {
      jpg: 'image/jpeg', jpeg: 'image/jpeg',
      png: 'image/png', webp: 'image/webp',
    };
    const ext = raw.name.split('.').pop()?.toLowerCase() ?? '';
    const resolvedType = raw.type || mimeMap[ext] || 'image/jpeg';
    const file = resolvedType !== raw.type ? new File([raw], raw.name, { type: resolvedType }) : raw;

    this.imageState.set({ kind: 'uploading' });
    this.client
      .uploadImage(this.id(), file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.imageState.set({ kind: 'idle' });
          this.recipe.reload();
        },
        error: (err: unknown) => {
          this.imageState.set({ kind: 'idle' });
          this.toast.show('error', getErrorMessage(err, this.translate, 'Failed to upload image.'));
        },
      });
  }

  protected onDeleteImage(): void {
    if (!window.confirm(this.translate.instant('recipes.confirmDeleteImage'))) return;

    this.imageState.set({ kind: 'deleting' });
    this.client
      .deleteImage(this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.imageState.set({ kind: 'idle' });
          this.recipe.reload();
        },
        error: (err: unknown) => {
          this.imageState.set({ kind: 'idle' });
          this.toast.show('error', getErrorMessage(err, this.translate, 'Failed to delete image.'));
        },
      });
  }

  // --- Cooking history ---

  protected readonly cookingHistory = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.client.getCookingHistory(params),
  });

  protected readonly isLoggingCooking = signal(false);
  protected readonly deletingCookingEntryId = signal<string | null>(null);

  protected onLogCooking(value: LogCookingValue): void {
    this.isLoggingCooking.set(true);
    this.client
      .logCooking({ recipeId: this.id(), ...value })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isLoggingCooking.set(false);
          this.cookingHistory.reload();
        },
        error: () => this.isLoggingCooking.set(false),
      });
  }

  protected onDeleteCookingEntry(id: string): void {
    if (!window.confirm(this.translate.instant('cookingLog.confirmDelete'))) return;

    this.deletingCookingEntryId.set(id);
    this.client
      .deleteCookingEntry(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deletingCookingEntryId.set(null);
          this.cookingHistory.reload();
        },
        error: () => this.deletingCookingEntryId.set(null),
      });
  }
}
