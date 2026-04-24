import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { RecipesClient } from '../../api/recipes.client';
import { IngredientSubstitutionSuggestionDto } from '../../api/recipes.dto';

type SubstitutionState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'loading' }
  | { readonly kind: 'error'; readonly message: string }
  | { readonly kind: 'success'; readonly result: IngredientSubstitutionSuggestionDto };

@Component({
  selector: 'app-suggest-substitutions-form',
  imports: [ReactiveFormsModule],
  templateUrl: './suggest-substitutions-form.html',
  styleUrl: './suggest-substitutions-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SuggestSubstitutionsForm {
  private readonly client = inject(RecipesClient);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = new FormGroup({
    ingredientName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    recipeContext: new FormControl('', { nonNullable: true }),
    dietaryGoal: new FormControl('', { nonNullable: true }),
  });

  private readonly state = signal<SubstitutionState>({ kind: 'idle' });

  protected readonly isLoading = computed(() => this.state().kind === 'loading');

  protected readonly submitError = computed(() => {
    const s = this.state();
    return s.kind === 'error' ? s.message : '';
  });

  protected readonly result = computed(() => {
    const s = this.state();
    return s.kind === 'success' ? s.result : null;
  });

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { ingredientName, recipeContext, dietaryGoal } = this.form.getRawValue();

    this.state.set({ kind: 'loading' });

    this.client
      .suggestSubstitutions({
        ingredientName,
        recipeContext: recipeContext || undefined,
        dietaryGoal: dietaryGoal || undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.state.set({ kind: 'success', result });
        },
        error: (err: unknown) => {
          this.state.set({ kind: 'error', message: this.toMessage(err) });
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
    return 'Failed to fetch substitutions.';
  }
}
