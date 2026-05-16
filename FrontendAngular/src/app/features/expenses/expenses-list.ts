import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { ToastService } from '../../core/toast.service';
import { ExpensesClient } from '../../api/expenses.client';
import { getErrorMessage } from '../../shared/get-error-message';

type SubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

type DeleteState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'deleting'; readonly id: string }
  | { readonly kind: 'error'; readonly id: string; readonly message: string };

@Component({
  selector: 'app-expenses-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ReactiveFormsModule, DecimalPipe, DatePipe, TranslateModule],
  templateUrl: './expenses-list.html',
  styleUrl: './expenses-list.css',
})
export class ExpensesList {
  private readonly client = inject(ExpensesClient);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

  protected readonly expenses = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly isEmpty = computed(
    () => !this.expenses.isLoading() && (this.expenses.value()?.length ?? 0) === 0,
  );

  protected readonly errorMessage = computed(() => {
    const err = this.expenses.error();
    return err ? getErrorMessage(err, this.translate) : '';
  });

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

  private readonly deleteState = signal<DeleteState>({ kind: 'idle' });

  protected isDeletingRow(id: string): boolean {
    const s = this.deleteState();
    return s.kind === 'deleting' && s.id === id;
  }

  protected onDeleteExpense(id: string): void {
    const confirmed = window.confirm(this.translate.instant('expenses.confirmDelete'));
    if (!confirmed) return;

    this.deleteState.set({ kind: 'deleting', id });
    this.client
      .delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deleteState.set({ kind: 'idle' });
          this.expenses.reload();
        },
        error: (err: unknown) => {
          this.deleteState.set({ kind: 'error', id, message: getErrorMessage(err, this.translate, 'Failed to delete') });
          this.toast.show('error', getErrorMessage(err, this.translate, 'Failed to delete'));
        },
      });
  }

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
          this.toast.show('success', this.translate.instant('expenses.expenseRecorded'));
        },
        error: (err: unknown) => {
          this.submitState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed to create') });
        },
      });
  }
}
