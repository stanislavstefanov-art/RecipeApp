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
import { MealPlansClient } from '../../api/meal-plans.client';

type SubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-meal-plans-create',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './meal-plans-create.html',
  styleUrl: './meal-plans-create.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlansCreate {
  private readonly client = inject(MealPlansClient);
  private readonly householdsClient = inject(HouseholdsClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly households = rxResource({
    stream: () => this.householdsClient.list(),
  });

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    householdId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  private readonly submitState = signal<SubmitState>({ kind: 'idle' });

  protected readonly isSubmitting = computed(() => this.submitState().kind === 'submitting');

  protected readonly submitError = computed(() => {
    const state = this.submitState();
    return state.kind === 'error' ? state.message : '';
  });

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    this.submitState.set({ kind: 'submitting' });

    this.client
      .create({ name: v.name, householdId: v.householdId })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          void this.router.navigate(['/meal-plans', response.id]);
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
    return 'Failed to create meal plan.';
  }
}
