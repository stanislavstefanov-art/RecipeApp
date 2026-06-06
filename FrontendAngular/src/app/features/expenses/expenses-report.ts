import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, resource, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom, map } from 'rxjs';

import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { ExpensesClient } from '../../api/expenses.client';

@Component({
  selector: 'app-expenses-report',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DecimalPipe, TranslateModule],
  templateUrl: './expenses-report.html',
  styleUrl: './expenses-report.css',
})
export class ExpensesReport {
  private readonly client = inject(ExpensesClient);
  private readonly translate = inject(TranslateService);

  private readonly now = new Date();

  private readonly lang = toSignal(
    this.translate.onLangChange.pipe(map((e) => e.lang)),
    { initialValue: this.translate.currentLang ?? 'en' },
  );

  protected readonly months = computed(() => {
    const lang = this.lang();
    return Array.from({ length: 12 }, (_, i) => ({
      value: i + 1,
      label: new Intl.DateTimeFormat(lang, { month: 'long' }).format(new Date(2000, i, 1)),
    }));
  });

  protected readonly query = signal({
    year: this.now.getFullYear(),
    month: this.now.getMonth() + 1,
  });

  protected readonly report = resource({
    params: () => this.query(),
    loader: ({ params }) => firstValueFrom(this.client.monthlyReport(params.year, params.month)),
  });

  private readonly insightsTrigger = signal<{ year: number; month: number } | null>(null);

  protected readonly insightsRequested = computed(() => this.insightsTrigger() !== null);

  protected readonly insights = resource({
    params: () => this.insightsTrigger(),
    loader: ({ params }) =>
      params ? firstValueFrom(this.client.insights(params.year, params.month)) : Promise.resolve(null),
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
    this.query.set({ year: +year, month: +month });
    this.insightsTrigger.set(null);
  }

  protected onAnalyse(): void {
    const q = this.query();
    const current = this.insightsTrigger();
    if (current?.year === q.year && current?.month === q.month) {
      this.insights.reload();
    } else {
      this.insightsTrigger.set(q);
    }
  }
}
