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

import { TranslateModule } from '@ngx-translate/core';

import { HouseholdsClient } from '../../api/households.client';
import { extractApiError } from '../../core/api-error';

type SubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-households-create',
  imports: [ReactiveFormsModule, RouterLink, TranslateModule],
  templateUrl: './households-create.html',
  styleUrl: './households-create.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HouseholdsCreate {
  private readonly client = inject(HouseholdsClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
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
      .create({ name: v.name })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          void this.router.navigate(['/households', response.id]);
        },
        error: (err: unknown) => {
          this.submitState.set({ kind: 'error', message: extractApiError(err) });
        },
      });
  }
}
