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
import { Router, RouterLink } from '@angular/router';

import { PersonsClient } from '../../api/persons.client';

type SubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-persons-create',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './persons-create.html',
  styleUrl: './persons-create.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonsCreate {
  private readonly client = inject(PersonsClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    vegetarian: new FormControl(false, { nonNullable: true }),
    pescatarian: new FormControl(false, { nonNullable: true }),
    vegan: new FormControl(false, { nonNullable: true }),
    highProtein: new FormControl(false, { nonNullable: true }),
    diabetes: new FormControl(false, { nonNullable: true }),
    highBloodPressure: new FormControl(false, { nonNullable: true }),
    glutenIntolerance: new FormControl(false, { nonNullable: true }),
    notes: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(1000)],
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

    const v = this.form.getRawValue();

    const dietaryPreferences: number[] = [];
    if (v.vegetarian) dietaryPreferences.push(1);
    if (v.pescatarian) dietaryPreferences.push(2);
    if (v.vegan) dietaryPreferences.push(3);
    if (v.highProtein) dietaryPreferences.push(4);

    const healthConcerns: number[] = [];
    if (v.diabetes) healthConcerns.push(1);
    if (v.highBloodPressure) healthConcerns.push(2);
    if (v.glutenIntolerance) healthConcerns.push(3);

    this.submitState.set({ kind: 'submitting' });

    this.client
      .create({
        name: v.name,
        dietaryPreferences,
        healthConcerns,
        notes: v.notes || undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          void this.router.navigate(['/persons']);
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
    return 'Failed to create person.';
  }
}
