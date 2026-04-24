import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { RecipesClient } from '../../api/recipes.client';

type SubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-add-step-form',
  imports: [ReactiveFormsModule],
  templateUrl: './add-step-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddStepForm {
  private readonly client = inject(RecipesClient);
  private readonly destroyRef = inject(DestroyRef);

  readonly recipeId = input.required<string>();
  readonly added = output<void>();

  protected readonly form = new FormGroup({
    instruction: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(1000)],
    }),
  });

  private readonly submitState = signal<SubmitState>({ kind: 'idle' });

  protected readonly isSubmitting = computed(
    () => this.submitState().kind === 'submitting',
  );

  protected readonly submitError = computed(() => {
    const state = this.submitState();
    return state.kind === 'error' ? state.message : '';
  });

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitState.set({ kind: 'submitting' });
    const payload = { instruction: this.form.controls.instruction.value };

    this.client
      .addStep(this.recipeId(), payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitState.set({ kind: 'idle' });
          this.form.reset({ instruction: '' });
          this.added.emit();
        },
        error: (err: unknown) => {
          this.submitState.set({ kind: 'error', message: this.toMessage(err) });
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
    return 'Failed to add step.';
  }
}
