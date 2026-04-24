import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { ExpensesClient } from '../../api/expenses.client';
import { CATEGORY_LABELS } from './expenses-list';

@Component({
  selector: 'app-expenses-report',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ReactiveFormsModule, DecimalPipe],
  templateUrl: './expenses-report.html',
  styleUrl: './expenses-report.css',
})
export class ExpensesReport {
  private readonly client = inject(ExpensesClient);
  protected readonly categoryLabels = CATEGORY_LABELS;

  private readonly now = new Date();

  protected readonly query = signal({
    year: this.now.getFullYear(),
    month: this.now.getMonth() + 1,
  });

  protected readonly report = rxResource({
    params: () => this.query(),
    stream: ({ params }) => this.client.monthlyReport(params.year, params.month),
  });

  protected readonly insights = rxResource({
    params: () => this.query(),
    stream: ({ params }) => this.client.insights(params.year, params.month),
  });

  protected readonly queryForm = new FormGroup({
    year: new FormControl(this.now.getFullYear(), {
      nonNullable: true,
      validators: [Validators.required, Validators.min(2000)],
    }),
    month: new FormControl(this.now.getMonth() + 1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(12)],
    }),
  });

  protected onSubmit(): void {
    if (this.queryForm.invalid) return;
    const { year, month } = this.queryForm.getRawValue();
    this.query.set({ year, month });
  }
}
