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
import { forkJoin, map, of, switchMap } from 'rxjs';

import { HouseholdsClient } from '../../api/households.client';
import { HouseholdListItemDto } from '../../api/households.dto';
import { RecipesClient } from '../../api/recipes.client';
import { ImportedRecipeDto } from '../../api/recipes.dto';
import { extractApiError } from '../../core/api-error';

type ImportState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'loading' }
  | { readonly kind: 'error'; readonly message: string }
  | { readonly kind: 'success'; readonly result: ImportedRecipeDto };

type SaveState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'saving' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-recipes-import',
  imports: [ReactiveFormsModule, RouterLink, TranslateModule],
  templateUrl: './recipes-import.html',
  styleUrl: './recipes-import.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipesImport {
  private readonly client = inject(RecipesClient);
  private readonly householdsClient = inject(HouseholdsClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = new FormGroup({
    recipeText: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(10)],
    }),
  });

  protected readonly saveForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    householdId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  protected readonly households = signal<HouseholdListItemDto[]>([]);
  protected readonly showHouseholdSelect = computed(() => this.households().length > 1);

  private readonly importState = signal<ImportState>({ kind: 'idle' });
  private readonly saveState = signal<SaveState>({ kind: 'idle' });

  protected readonly isLoading = computed(() => this.importState().kind === 'loading');
  protected readonly isSaving = computed(() => this.saveState().kind === 'saving');

  protected readonly submitError = computed(() => {
    const s = this.importState();
    return s.kind === 'error' ? s.message : '';
  });

  protected readonly saveError = computed(() => {
    const s = this.saveState();
    return s.kind === 'error' ? s.message : '';
  });

  protected readonly result = computed(() => {
    const s = this.importState();
    return s.kind === 'success' ? s.result : null;
  });

  constructor() {
    this.householdsClient
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.households.set(items);
          if (items.length === 1) {
            this.saveForm.controls.householdId.setValue(items[0].id);
          }
        },
      });
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.importState.set({ kind: 'loading' });
    this.saveState.set({ kind: 'idle' });

    this.client
      .importFromText({ text: this.form.controls.recipeText.value })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.importState.set({ kind: 'success', result });
          this.saveForm.controls.name.setValue(result.title ?? '');
        },
        error: (err: unknown) => {
          this.importState.set({ kind: 'error', message: extractApiError(err) });
        },
      });
  }

  protected onSave(): void {
    if (this.saveForm.invalid) {
      this.saveForm.markAllAsTouched();
      return;
    }

    const { name, householdId } = this.saveForm.getRawValue();
    this.saveState.set({ kind: 'saving' });

    const extracted = this.result()!;

    this.client
      .create({ name, householdId, recipeType: 1, isImported: true })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap((response) => {
          const id = response.id;
          const ingredientCalls = extracted.ingredients.map((ing) =>
            this.client.addIngredient(id, {
              name: ing.name,
              quantity: parseFloat(ing.quantity ?? '1') || 1,
              unit: ing.unit ?? '',
            }),
          );
          const stepCalls = extracted.steps.map((step) =>
            this.client.addStep(id, { instruction: step }),
          );
          const all = [...ingredientCalls, ...stepCalls];
          if (all.length === 0) return of(id);
          return forkJoin(all).pipe(map(() => id));
        }),
      )
      .subscribe({
        next: (id) => {
          void this.router.navigate(['/recipes', id]);
        },
        error: (err: unknown) => {
          this.saveState.set({ kind: 'error', message: extractApiError(err) });
        },
      });
  }
}
