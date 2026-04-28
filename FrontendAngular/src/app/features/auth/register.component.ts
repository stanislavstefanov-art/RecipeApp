import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthClient } from '../../api/auth.client';
import { login } from '../../core/auth.store';
import { getErrorMessage } from '../../shared/get-error-message';

function hasLetter(ctrl: AbstractControl): ValidationErrors | null {
  return /[a-zA-Z]/.test(ctrl.value as string) ? null : { passwordLetter: true };
}

function hasDigit(ctrl: AbstractControl): ValidationErrors | null {
  return /[0-9]/.test(ctrl.value as string) ? null : { passwordDigit: true };
}

type SubmitState = { kind: 'idle' } | { kind: 'submitting' } | { kind: 'error'; message: string };

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, TranslateModule, RouterLink],
  templateUrl: './register.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  private readonly client = inject(AuthClient);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = new FormGroup({
    displayName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8), hasLetter, hasDigit],
    }),
  });

  private readonly submitState = signal<SubmitState>({ kind: 'idle' });
  protected readonly isSubmitting = computed(() => this.submitState().kind === 'submitting');
  protected readonly submitError = computed(() => {
    const s = this.submitState();
    return s.kind === 'error' ? s.message : '';
  });

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitState.set({ kind: 'submitting' });
    const values = this.form.getRawValue();

    this.client.register(values)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => {
          login(session);
          void this.router.navigate(['/']);
        },
        error: (err: unknown) => {
          this.submitState.set({ kind: 'error', message: getErrorMessage(err, this.translate) });
        },
      });
  }
}
