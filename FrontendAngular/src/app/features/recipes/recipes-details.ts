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
import { FormsModule } from '@angular/forms';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { RecipesClient } from '../../api/recipes.client';
import { getErrorMessage } from '../../shared/get-error-message';
import { StarRatingComponent } from '../../shared/ui/star-rating/star-rating';
import { AddIngredientForm } from './add-ingredient-form';
import { AddStepForm } from './add-step-form';
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

@Component({
  selector: 'app-recipes-details',
  imports: [RouterLink, FormsModule, UpdateRecipeNameForm, AddIngredientForm, AddStepForm, SuggestSubstitutionsForm, TranslateModule, StarRatingComponent],
  templateUrl: './recipes-details.html',
  styleUrl: './recipes-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipesDetails {
  private readonly client = inject(RecipesClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

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
    const err = this.recipe.error();
    return err instanceof HttpErrorResponse && err.status === 404;
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
}
