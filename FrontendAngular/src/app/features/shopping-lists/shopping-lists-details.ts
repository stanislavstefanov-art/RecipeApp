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
import { RouterLink } from '@angular/router';

import { MealPlansClient } from '../../api/meal-plans.client';
import { ShoppingListsClient } from '../../api/shopping-lists.client';
import { ShoppingListDetailsItemDto } from '../../api/shopping-lists.dto';
import { SOURCE_TYPE_LABELS } from './shopping-lists-list';

type ItemActionState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'busy'; readonly itemId: string }
  | { readonly kind: 'error'; readonly itemId: string; readonly message: string };

type GenerateState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'busy' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-shopping-lists-details',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './shopping-lists-details.html',
  styleUrl: './shopping-lists-details.css',
})
export class ShoppingListsDetails {
  protected readonly sourceTypeLabels = SOURCE_TYPE_LABELS;

  readonly id = input.required<string>();

  private readonly client = inject(ShoppingListsClient);
  private readonly mealPlansClient = inject(MealPlansClient);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly shoppingList = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.client.get(params),
  });

  protected readonly mealPlans = rxResource({
    stream: () => this.mealPlansClient.list(),
  });

  protected readonly is404 = computed(() => {
    const err = this.shoppingList.error() as { status?: number } | null;
    return err?.status === 404;
  });

  protected readonly errorMessage = computed(
    () => (this.shoppingList.error() as Error)?.message ?? 'Unknown error',
  );

  protected readonly markPendingState = signal<ItemActionState>({ kind: 'idle' });
  protected readonly generateState = signal<GenerateState>({ kind: 'idle' });
  protected readonly regenerateState = signal<GenerateState>({ kind: 'idle' });

  protected readonly purchasingItem = signal<ShoppingListDetailsItemDto | null>(null);

  protected readonly purchaseForm = new FormGroup({
    amount: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.min(0.01)] }),
    currency: new FormControl('USD', { nonNullable: true, validators: [Validators.required] }),
    expenseDate: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true }),
  });

  protected readonly purchaseState = signal<GenerateState>({ kind: 'idle' });

  protected readonly selectedMealPlanId = new FormControl('', { nonNullable: true });

  protected onMarkPending(item: ShoppingListDetailsItemDto): void {
    this.markPendingState.set({ kind: 'busy', itemId: item.id });
    this.client
      .markPending(this.id(), item.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.markPendingState.set({ kind: 'idle' });
          this.shoppingList.reload();
        },
        error: (err: Error) => {
          this.markPendingState.set({
            kind: 'error',
            itemId: item.id,
            message: err.message ?? 'Failed',
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
          this.shoppingList.reload();
        },
        error: (err: Error) => {
          this.purchaseState.set({ kind: 'error', message: err.message ?? 'Failed' });
        },
      });
  }

  protected onGenerate(): void {
    const mealPlanId = this.selectedMealPlanId.value;
    if (!mealPlanId) return;
    this.generateState.set({ kind: 'busy' });

    this.mealPlansClient
      .generateFromMealPlan(mealPlanId, this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.generateState.set({ kind: 'idle' });
          this.shoppingList.reload();
        },
        error: (err: Error) => {
          this.generateState.set({ kind: 'error', message: err.message ?? 'Failed' });
        },
      });
  }

  protected onRegenerate(): void {
    const mealPlanId = this.selectedMealPlanId.value;
    if (!mealPlanId) return;
    this.regenerateState.set({ kind: 'busy' });

    this.mealPlansClient
      .regenerateFromMealPlan(mealPlanId, this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.regenerateState.set({ kind: 'idle' });
          this.shoppingList.reload();
        },
        error: (err: Error) => {
          this.regenerateState.set({ kind: 'error', message: err.message ?? 'Failed' });
        },
      });
  }
}
