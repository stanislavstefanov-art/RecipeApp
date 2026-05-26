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

import { HouseholdListItemDto } from '../../api/households.dto';
import { HouseholdsClient } from '../../api/households.client';
import { RecipesClient } from '../../api/recipes.client';
import { extractApiError } from '../../core/api-error';

type HouseholdsState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'loaded'; readonly items: HouseholdListItemDto[] }
  | { readonly kind: 'error' };

type SubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-recipes-create',
  imports: [ReactiveFormsModule, RouterLink, TranslateModule],
  templateUrl: './recipes-create.html',
  styleUrl: './recipes-create.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipesCreate {
  private readonly recipesClient = inject(RecipesClient);
  private readonly householdsClient = inject(HouseholdsClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    householdId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    recipeType: new FormControl('1', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    difficultyLevel: new FormControl<string>('', { nonNullable: true }),
  });

  private readonly householdsState = signal<HouseholdsState>({ kind: 'loading' });

  protected readonly households = computed(() => {
    const s = this.householdsState();
    return s.kind === 'loaded' ? s.items : [];
  });
  protected readonly householdsLoading = computed(() => this.householdsState().kind === 'loading');
  protected readonly householdsError = computed(() => this.householdsState().kind === 'error');
  protected readonly noHouseholds = computed(
    () => this.householdsState().kind === 'loaded' && this.households().length === 0,
  );
  protected readonly showHouseholdSelect = computed(() => this.households().length > 1);
  protected readonly canSubmit = computed(
    () => !this.householdsLoading() && !this.householdsError() && !this.noHouseholds(),
  );

  private readonly submitState = signal<SubmitState>({ kind: 'idle' });

  protected readonly isSubmitting = computed(
    () => this.submitState().kind === 'submitting',
  );

  protected readonly submitError = computed(() => {
    const state = this.submitState();
    return state.kind === 'error' ? state.message : '';
  });

  constructor() {
    this.householdsClient
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.householdsState.set({ kind: 'loaded', items });
          if (items.length === 1) {
            this.form.controls.householdId.setValue(items[0].id);
          }
        },
        error: () => this.householdsState.set({ kind: 'error' }),
      });
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitState.set({ kind: 'submitting' });
    const { name, householdId, recipeType, difficultyLevel } = this.form.getRawValue();
    const difficulty = difficultyLevel ? parseInt(difficultyLevel, 10) : null;

    this.recipesClient
      .create({ name, householdId, recipeType: parseInt(recipeType, 10), difficultyLevel: difficulty })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          void this.router.navigate(['/recipes', response.id]);
        },
        error: (err: unknown) => {
          this.submitState.set({ kind: 'error', message: extractApiError(err) });
        },
      });
  }
}
