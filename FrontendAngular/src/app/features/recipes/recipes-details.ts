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
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { RecipesClient } from '../../api/recipes.client';
import { getErrorMessage } from '../../shared/get-error-message';
import { AddIngredientForm } from './add-ingredient-form';
import { AddStepForm } from './add-step-form';
import { SuggestSubstitutionsForm } from './suggest-substitutions-form';
import { UpdateRecipeNameForm } from './update-recipe-name-form';

type DeleteState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'deleting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-recipes-details',
  imports: [RouterLink, UpdateRecipeNameForm, AddIngredientForm, AddStepForm, SuggestSubstitutionsForm, TranslateModule],
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
}
