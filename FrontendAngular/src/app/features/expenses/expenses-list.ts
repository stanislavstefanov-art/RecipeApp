import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { ExpensesClient } from '../../api/expenses.client';

type SubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

export const CATEGORY_LABELS: Readonly<Record<number, string>> = {
  1: 'Food',
  2: 'Transport',
  3: 'Utilities',
  4: 'Entertainment',
  5: 'Health',
  6: 'Other',
};

export const EXPENSE_SOURCE_TYPE_LABELS: Readonly<Record<number, string>> = {
  1: 'Manual',
  2: 'Shopping list item',
  3: 'Meal plan',
};

@Component({
  selector: 'app-expenses-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ReactiveFormsModule, DecimalPipe, DatePipe],
  templateUrl: './expenses-list.html',
  styleUrl: './expenses-list.css',
})
export class ExpensesList {
  private readonly client = inject(ExpensesClient);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly categoryLabels = CATEGORY_LABELS;

  protected readonly expenses = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly isEmpty = computed(
    () => !this.expenses.isLoading() && (this.expenses.value()?.length ?? 0) === 0,
  );

  protected readonly errorMessage = computed(
    () => (this.expenses.error() as Error)?.message ?? 'Unknown error',
  );

  protected readonly createForm = new FormGroup({
    amount: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0.01)],
    }),
    currency: new FormControl('USD', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    expenseDate: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    category: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    description: new FormControl('', { nonNullable: true }),
  });

  protected readonly submitState = signal<SubmitState>({ kind: 'idle' });

  protected onSubmit(): void {
    if (this.createForm.invalid) return;
    const { amount, currency, expenseDate, category, description } =
      this.createForm.getRawValue();
    this.submitState.set({ kind: 'submitting' });

    this.client
      .create({
        amount: parseFloat(amount),
        currency,
        expenseDate,
        category: parseInt(category, 10),
        description: description || undefined,
        sourceType: 1,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.createForm.reset({ currency: 'USD' });
          this.submitState.set({ kind: 'idle' });
          this.expenses.reload();
        },
        error: (err: Error) => {
          this.submitState.set({ kind: 'error', message: err.message ?? 'Failed to create' });
        },
      });
  }
}
