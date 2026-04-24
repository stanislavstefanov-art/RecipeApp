import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';

import { ShoppingListsClient } from '../../api/shopping-lists.client';

type SubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

export const SOURCE_TYPE_LABELS: Readonly<Record<number, string>> = {
  1: 'Manual',
  2: 'Recipe',
  3: 'Meal plan',
};

@Component({
  selector: 'app-shopping-lists-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './shopping-lists-list.html',
  styleUrl: './shopping-lists-list.css',
})
export class ShoppingListsList {
  private readonly client = inject(ShoppingListsClient);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly shoppingLists = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly isEmpty = computed(
    () => !this.shoppingLists.isLoading() && (this.shoppingLists.value()?.length ?? 0) === 0,
  );

  protected readonly errorMessage = computed(
    () => (this.shoppingLists.error() as Error)?.message ?? 'Unknown error',
  );

  protected readonly nameControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(1)],
  });

  protected readonly submitState = signal<SubmitState>({ kind: 'idle' });

  protected onSubmit(): void {
    if (this.nameControl.invalid) return;
    this.submitState.set({ kind: 'submitting' });

    this.client
      .create({ name: this.nameControl.value })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.nameControl.reset();
          this.submitState.set({ kind: 'idle' });
          this.shoppingLists.reload();
        },
        error: (err: Error) => {
          this.submitState.set({ kind: 'error', message: err.message ?? 'Failed to create' });
        },
      });
  }
}
