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
import { PersonsClient } from '../../api/persons.client';
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
  selector: 'app-persons-create',
  imports: [ReactiveFormsModule, RouterLink, TranslateModule],
  templateUrl: './persons-create.html',
  styleUrl: './persons-create.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonsCreate {
  private readonly client = inject(PersonsClient);
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
        householdId: v.householdId,
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
          this.submitState.set({ kind: 'error', message: extractApiError(err) });
        },
      });
  }
}
