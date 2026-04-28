import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthClient } from '../../api/auth.client';
import { login } from '../../core/auth.store';
import { getErrorMessage } from '../../shared/get-error-message';

type SubmitState = { kind: 'idle' } | { kind: 'submitting' } | { kind: 'error'; message: string };

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, TranslateModule, RouterLink],
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private readonly client = inject(AuthClient);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
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
    const { email, password } = this.form.getRawValue();

    this.client.login({ email, password })
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
