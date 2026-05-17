import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { MealPlansClient } from '../../api/meal-plans.client';
import { getErrorMessage } from '../../shared/get-error-message';

@Component({
  selector: 'app-meal-plans-list',
  imports: [RouterLink, TranslateModule],
  templateUrl: './meal-plans-list.html',
  styleUrl: './meal-plans-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlansList {
  private readonly client = inject(MealPlansClient);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly mealPlans = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly deletingId = signal<string | null>(null);

  protected readonly errorMessage = computed(() => {
    const err = this.mealPlans.error();
    return err ? getErrorMessage(err, this.translate) : '';
  });

  protected readonly isEmpty = computed(() => {
    const value = this.mealPlans.value();
    return value !== undefined && value.length === 0;
  });

  protected onDelete(id: string, name: string, event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (!window.confirm(this.translate.instant('mealPlans.confirmDelete', { name }))) return;

    this.deletingId.set(id);
    this.client
      .delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deletingId.set(null);
          this.mealPlans.reload();
        },
        error: () => this.deletingId.set(null),
      });
  }
}
