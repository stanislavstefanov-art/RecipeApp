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
import { RouterLink } from '@angular/router';

import { RecipesClient } from '../../api/recipes.client';
import { ImportedRecipeDto } from '../../api/recipes.dto';

type ImportState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'loading' }
  | { readonly kind: 'error'; readonly message: string }
  | { readonly kind: 'success'; readonly result: ImportedRecipeDto };

@Component({
  selector: 'app-recipes-import',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './recipes-import.html',
  styleUrl: './recipes-import.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipesImport {
  private readonly client = inject(RecipesClient);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = new FormGroup({
    recipeText: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(10)],
    }),
  });

  private readonly state = signal<ImportState>({ kind: 'idle' });

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

    this.state.set({ kind: 'loading' });

    this.client
      .importFromText({ text: this.form.controls.recipeText.value })
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
    return 'Failed to extract recipe.';
  }
}
