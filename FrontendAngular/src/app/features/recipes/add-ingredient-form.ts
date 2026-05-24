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
import { takeUntilDestroyed, rxResource } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';

import { TranslateModule } from '@ngx-translate/core';

import { RecipesClient } from '../../api/recipes.client';
import { UnitsClient } from '../../api/units.client';
import { UnitNamePipe } from '../../shared/unit-name.pipe';
import { extractApiError } from '../../core/api-error';

type SubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

function greaterThanZero(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (value === null || value === undefined || value === '') {
    return null;
  }
  return typeof value === 'number' && value > 0 ? null : { greaterThanZero: true };
}

const INITIAL_VALUES = { name: '', quantity: 1, unit: '' };

@Component({
  selector: 'app-add-ingredient-form',
  imports: [ReactiveFormsModule, TranslateModule, UnitNamePipe],
  templateUrl: './add-ingredient-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddIngredientForm {
  private readonly client = inject(RecipesClient);
  private readonly unitsClient = inject(UnitsClient);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly units = rxResource({
    stream: () => this.unitsClient.list(),
  });

  readonly recipeId = input.required<string>();
  readonly added = output<void>();

  protected readonly form = new FormGroup({
    name: new FormControl(INITIAL_VALUES.name, {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    quantity: new FormControl<number | null>(INITIAL_VALUES.quantity, {
      validators: [Validators.required, greaterThanZero],
    }),
    unit: new FormControl(INITIAL_VALUES.unit, {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50)],
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

    const { name, quantity, unit } = this.form.getRawValue();
    if (quantity === null) {
      return;
    }

    this.submitState.set({ kind: 'submitting' });

    this.client
      .addIngredient(this.recipeId(), { name, quantity, unit })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitState.set({ kind: 'idle' });
          this.form.reset(INITIAL_VALUES);
          this.added.emit();
        },
        error: (err: unknown) => {
          this.submitState.set({ kind: 'error', message: extractApiError(err) });
        },
      });
  }
}
