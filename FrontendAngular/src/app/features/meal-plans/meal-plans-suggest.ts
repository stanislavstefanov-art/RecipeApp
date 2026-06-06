import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';

import { TranslateModule } from '@ngx-translate/core';

import { HouseholdsClient } from '../../api/households.client';
import { MealPlanSuggestionDto } from '../../api/meal-plans.dto';
import { MealPlansClient } from '../../api/meal-plans.client';
import { RecipesClient } from '../../api/recipes.client';
import { extractApiError } from '../../core/api-error';

type SuggestState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'loading' }
  | { readonly kind: 'error'; readonly message: string }
  | { readonly kind: 'success'; readonly result: MealPlanSuggestionDto };

type AcceptState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-meal-plans-suggest',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, TranslateModule],
  templateUrl: './meal-plans-suggest.html',
  styleUrl: './meal-plans-suggest.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlansSuggest {
  private readonly client = inject(MealPlansClient);
  private readonly householdsClient = inject(HouseholdsClient);
  private readonly recipesClient = inject(RecipesClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly households = rxResource({
    stream: () => this.householdsClient.list(),
  });

  protected readonly recipes = rxResource({
    stream: () => this.recipesClient.list(),
  });

  protected readonly selectedHouseholdId = signal('');

  protected readonly householdMembers = rxResource({
    params: () => this.selectedHouseholdId(),
    stream: ({ params }) => params ? this.householdsClient.get(params) : of(null),
  });

  protected readonly members = computed(() => this.householdMembers.value()?.members ?? []);

  // mealType → Set of excluded personIds
  protected readonly excludedPersons = signal<Record<number, Set<string>>>({});

  protected readonly recipeMap = computed(
    () => new Map((this.recipes.value() ?? []).map((r) => [r.id, r.name])),
  );

  protected readonly priorityIngredients = signal<string[]>([]);
  protected readonly ingredientQuery = signal('');

  private readonly allIngredients = computed(() => {
    const set = new Set<string>();
    for (const r of this.recipes.value() ?? []) {
      for (const name of r.ingredientNames) set.add(name);
    }
    return [...set].sort((a, b) => a.localeCompare(b));
  });

  protected readonly ingredientSuggestions = computed(() => {
    const q = this.ingredientQuery().trim().toLowerCase();
    if (q.length < 2) return [];
    const selected = new Set(this.priorityIngredients().map((i) => i.toLowerCase()));
    return this.allIngredients()
      .filter((n) => n.toLowerCase().includes(q) && !selected.has(n.toLowerCase()))
      .slice(0, 8);
  });

  protected addIngredient(name: string): void {
    const trimmed = name.trim();
    if (!trimmed) return;
    if (!this.priorityIngredients().some((i) => i.toLowerCase() === trimmed.toLowerCase())) {
      this.priorityIngredients.update((list) => [...list, trimmed]);
    }
    this.ingredientQuery.set('');
  }

  protected removeIngredient(name: string): void {
    this.priorityIngredients.update((list) => list.filter((i) => i !== name));
  }

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    householdId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    startDate: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    numberOfDays: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(31)],
    }),
    breakfast: new FormControl(false, { nonNullable: true }),
    lunch: new FormControl(false, { nonNullable: true }),
    dinner: new FormControl(false, { nonNullable: true }),
    snack: new FormControl(false, { nonNullable: true }),
    recipeSource: new FormControl<'all' | 'manual' | 'imported'>('all', { nonNullable: true }),
    recipeOrigin: new FormControl<'all' | 'home' | 'borrowed'>('all', { nonNullable: true }),
  });

  protected readonly noMealTypeError = signal(false);
  protected readonly showInfo = signal(false);

  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  protected readonly activeMealTypes = computed(() => {
    const v = this.formValue();
    const active: number[] = [];
    if (v.breakfast) active.push(1);
    if (v.lunch) active.push(2);
    if (v.dinner) active.push(3);
    if (v.snack) active.push(4);
    return active;
  });

  private readonly suggestState = signal<SuggestState>({ kind: 'idle' });
  private readonly acceptState = signal<AcceptState>({ kind: 'idle' });

  protected readonly isSuggesting = computed(() => this.suggestState().kind === 'loading');
  protected readonly suggestError = computed(() => {
    const s = this.suggestState();
    return s.kind === 'error' ? s.message : '';
  });
  protected readonly suggestion = computed(() => {
    const s = this.suggestState();
    return s.kind === 'success' ? s.result : null;
  });

  protected readonly isAccepting = computed(() => this.acceptState().kind === 'submitting');
  protected readonly acceptError = computed(() => {
    const s = this.acceptState();
    return s.kind === 'error' ? s.message : '';
  });

  protected isPersonExcluded(mealType: number, personId: string): boolean {
    return this.excludedPersons()[mealType]?.has(personId) ?? false;
  }

  protected togglePersonExclusion(mealType: number, personId: string): void {
    this.excludedPersons.update((current) => {
      const updated = { ...current };
      const set = new Set(updated[mealType] ?? []);
      if (set.has(personId)) set.delete(personId);
      else set.add(personId);
      updated[mealType] = set;
      return updated;
    });
  }

  protected onHouseholdChange(id: string): void {
    this.selectedHouseholdId.set(id);
    this.excludedPersons.set({});
    this.form.controls.householdId.setValue(id);
  }

  protected onSuggest(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const mealTypes: number[] = [];
    if (v.breakfast) mealTypes.push(1);
    if (v.lunch) mealTypes.push(2);
    if (v.dinner) mealTypes.push(3);
    if (v.snack) mealTypes.push(4);

    if (mealTypes.length === 0) {
      this.noMealTypeError.set(true);
      return;
    }
    this.noMealTypeError.set(false);

    const allMembers = this.members();
    const excluded = this.excludedPersons();
    const personsPerMealType: Record<number, string[]> = {};
    for (const mt of mealTypes) {
      const excludedSet = excluded[mt];
      if (excludedSet && excludedSet.size > 0) {
        personsPerMealType[mt] = allMembers
          .filter((m) => !excludedSet.has(m.personId))
          .map((m) => m.personId);
      }
    }

    this.suggestState.set({ kind: 'loading' });

    this.client
      .suggest({
        name: v.name,
        householdId: v.householdId,
        startDate: v.startDate,
        numberOfDays: parseInt(v.numberOfDays, 10),
        mealTypes,
        recipeSource: v.recipeSource,
        recipeOrigin: v.recipeOrigin,
        personsPerMealType: Object.keys(personsPerMealType).length > 0 ? personsPerMealType : undefined,
        priorityIngredients: this.priorityIngredients().length > 0 ? this.priorityIngredients() : undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.suggestState.set({ kind: 'success', result });
          this.acceptState.set({ kind: 'idle' });
        },
        error: (err: unknown) => {
          this.suggestState.set({ kind: 'error', message: extractApiError(err) });
        },
      });
  }

  protected onAccept(): void {
    const s = this.suggestState();
    if (s.kind !== 'success') {
      return;
    }

    const v = this.form.getRawValue();
    const result = s.result;

    this.acceptState.set({ kind: 'submitting' });

    this.client
      .acceptSuggestion({
        name: result.name,
        householdId: v.householdId,
        entries: result.entries.map((e) => ({
          baseRecipeId: e.baseRecipeId,
          saladRecipeId: e.saladRecipeId,
          plannedDate: e.plannedDate,
          mealType: e.mealType,
          scope: e.scope,
          assignments: e.assignments.map((a) => ({
            personId: a.personId,
            assignedRecipeId: a.assignedRecipeId,
            recipeVariationId: a.recipeVariationId,
            portionMultiplier: a.portionMultiplier,
            notes: a.notes,
          })),
        })),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          void this.router.navigate(['/meal-plans', response.mealPlanId]);
        },
        error: (err: unknown) => {
          this.acceptState.set({ kind: 'error', message: extractApiError(err) });
        },
      });
  }
}
