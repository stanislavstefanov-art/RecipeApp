import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { ToastService } from '../../core/toast.service';
import { MealPlansClient } from '../../api/meal-plans.client';
import { RecipesClient } from '../../api/recipes.client';
import { ShoppingListsClient } from '../../api/shopping-lists.client';
import { UnitsClient } from '../../api/units.client';
import { UnitNamePipe } from '../../shared/unit-name.pipe';
import { ShoppingListDetailsItemDto } from '../../api/shopping-lists.dto';
import { getErrorMessage } from '../../shared/get-error-message';

type ItemActionState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'busy'; readonly itemId: string }
  | { readonly kind: 'error'; readonly itemId: string; readonly message: string };

type GenerateState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'busy' }
  | { readonly kind: 'error'; readonly message: string };

type DeleteState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'deleting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-shopping-lists-details',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ReactiveFormsModule, TranslateModule, UnitNamePipe],
  templateUrl: './shopping-lists-details.html',
  styleUrl: './shopping-lists-details.css',
})
export class ShoppingListsDetails {
  readonly id = input.required<string>();

  private readonly client = inject(ShoppingListsClient);
  private readonly mealPlansClient = inject(MealPlansClient);
  private readonly unitsClient = inject(UnitsClient);
  private readonly recipesClient = inject(RecipesClient);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

  private readonly _refresh = signal(0);

  protected readonly shoppingList = rxResource({
    params: () => ({ id: this.id(), r: this._refresh() }),
    stream: ({ params }) => this.client.get(params.id),
  });

  protected readonly mealPlans = rxResource({
    stream: () => this.mealPlansClient.list(),
  });

  protected readonly allRecipes = rxResource({
    stream: () => this.recipesClient.list(),
  });

  protected readonly boughtRecipes = computed(() =>
    (this.allRecipes.value() ?? []).filter((r) => r.origin === 3),
  );

  protected readonly selectedBoughtRecipeId = new FormControl('', { nonNullable: true });
  protected readonly addBoughtState = signal<GenerateState>({ kind: 'idle' });

  protected readonly units = rxResource({
    stream: () => this.unitsClient.list(),
  });

  protected readonly is404 = computed(() => {
    const err = this.shoppingList.error() as { status?: number } | null;
    return err?.status === 404;
  });

  protected readonly errorMessage = computed(() => {
    const err = this.shoppingList.error();
    return err ? getErrorMessage(err, this.translate) : '';
  });

  private readonly deleteState = signal<DeleteState>({ kind: 'idle' });

  protected readonly isDeleting = computed(() => this.deleteState().kind === 'deleting');
  protected readonly deleteError = computed(() => {
    const s = this.deleteState();
    return s.kind === 'error' ? s.message : '';
  });

  protected onDelete(): void {
    const confirmed = window.confirm(this.translate.instant('shoppingLists.confirmDelete'));
    if (!confirmed) return;

    this.deleteState.set({ kind: 'deleting' });
    this.client
      .delete(this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => void this.router.navigate(['/shopping-lists']),
        error: (err: unknown) => {
          this.deleteState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed to delete') });
        },
      });
  }

  protected readonly markPendingState = signal<ItemActionState>({ kind: 'idle' });
  protected readonly generateState = signal<GenerateState>({ kind: 'idle' });

  protected readonly expandedItemId = signal<string | null>(null);

  protected toggleRecipes(itemId: string): void {
    this.expandedItemId.update((current) => (current === itemId ? null : itemId));
  }

  protected readonly purchasingItem = signal<ShoppingListDetailsItemDto | null>(null);

  protected readonly purchaseForm = new FormGroup({
    amount: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.min(0.01)] }),
    currency: new FormControl('USD', { nonNullable: true, validators: [Validators.required] }),
    expenseDate: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true }),
  });

  protected readonly purchaseState = signal<GenerateState>({ kind: 'idle' });

  protected readonly selectedMealPlanId = new FormControl('', { nonNullable: true });

  protected readonly addItemForm = new FormGroup({
    productName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
    quantity: new FormControl<number | null>(null, { validators: [Validators.required, Validators.min(0.01)] }),
    unit: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50)] }),
  });

  protected readonly addItemState = signal<GenerateState>({ kind: 'idle' });

  protected onAddItem(): void {
    if (this.addItemForm.invalid) return;
    const { productName, quantity, unit } = this.addItemForm.getRawValue();
    this.addItemState.set({ kind: 'busy' });

    this.client
      .addManualItem(this.id(), { productName, quantity: quantity!, unit })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.addItemForm.reset();
          this.addItemState.set({ kind: 'idle' });
          this._refresh.update(n => n + 1);
          this.toast.show('success', this.translate.instant('shoppingLists.itemAdded'));
        },
        error: (err: unknown) => {
          this.addItemState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed') });
        },
      });
  }

  protected onMarkPending(item: ShoppingListDetailsItemDto): void {
    this.markPendingState.set({ kind: 'busy', itemId: item.id });
    this.client
      .markPending(this.id(), item.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.markPendingState.set({ kind: 'idle' });
          this._refresh.update(n => n + 1);
          this.toast.show('success', `"${item.productName}" marked as pending`);
        },
        error: (err: unknown) => {
          this.markPendingState.set({
            kind: 'error',
            itemId: item.id,
            message: getErrorMessage(err, this.translate, 'Failed'),
          });
        },
      });
  }

  protected onOpenPurchase(item: ShoppingListDetailsItemDto): void {
    this.purchasingItem.set(item);
    this.purchaseForm.reset({ currency: 'USD' });
    this.purchaseState.set({ kind: 'idle' });
  }

  protected onCancelPurchase(): void {
    this.purchasingItem.set(null);
  }

  protected onSubmitPurchase(): void {
    const item = this.purchasingItem();
    if (!item || this.purchaseForm.invalid) return;

    const { amount, currency, expenseDate, description } = this.purchaseForm.getRawValue();
    this.purchaseState.set({ kind: 'busy' });

    this.client
      .purchaseWithExpense(this.id(), item.id, {
        amount: parseFloat(amount),
        currency,
        expenseDate,
        description: description || undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.purchasingItem.set(null);
          this.purchaseState.set({ kind: 'idle' });
          this._refresh.update(n => n + 1);
          this.toast.show('success', this.translate.instant('shoppingLists.itemPurchased'));
        },
        error: (err: unknown) => {
          this.purchaseState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed') });
        },
      });
  }

  protected onAddBoughtRecipe(): void {
    const recipeId = this.selectedBoughtRecipeId.value;
    if (!recipeId) return;
    const recipe = this.boughtRecipes().find((r) => r.id === recipeId);
    if (!recipe) return;

    this.addBoughtState.set({ kind: 'busy' });
    this.client
      .addManualItem(this.id(), { productName: recipe.name, quantity: 1, unit: 'порция' })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.selectedBoughtRecipeId.reset();
          this.addBoughtState.set({ kind: 'idle' });
          this._refresh.update((n) => n + 1);
          this.toast.show('success', this.translate.instant('shoppingLists.itemAdded'));
        },
        error: (err: unknown) => {
          this.addBoughtState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed') });
        },
      });
  }

  protected onGenerate(): void {
    const mealPlanId = this.selectedMealPlanId.value;
    if (!mealPlanId) return;
    this.generateState.set({ kind: 'busy' });

    // Always use the idempotent regenerate path: it removes this meal plan's
    // previously generated items first, then re-adds them fresh — safe on an
    // empty list and safe to re-run without doubling quantities. Manual items
    // and items from other meal plans are preserved.
    this.mealPlansClient
      .regenerateFromMealPlan(mealPlanId, this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.generateState.set({ kind: 'idle' });
          this._refresh.update(n => n + 1);
          this.toast.show('success', this.translate.instant('shoppingLists.listGenerated'));
        },
        error: (err: unknown) => {
          this.generateState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed') });
        },
      });
  }
}
