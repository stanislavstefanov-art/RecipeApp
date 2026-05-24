import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { ToastService } from '../../core/toast.service';
import { ShoppingListsClient } from '../../api/shopping-lists.client';
import { HouseholdsClient } from '../../api/households.client';
import { getErrorMessage } from '../../shared/get-error-message';

type SubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-shopping-lists-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ReactiveFormsModule, TranslateModule],
  templateUrl: './shopping-lists-list.html',
  styleUrl: './shopping-lists-list.css',
})
export class ShoppingListsList {
  private readonly client = inject(ShoppingListsClient);
  private readonly householdsClient = inject(HouseholdsClient);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

  private readonly _refresh = signal(0);

  protected readonly shoppingLists = rxResource({
    params: () => this._refresh(),
    stream: () => this.client.list(),
  });

  protected readonly households = rxResource({
    stream: () => this.householdsClient.list(),
  });

  protected readonly isEmpty = computed(
    () => !this.shoppingLists.isLoading() && (this.shoppingLists.value()?.length ?? 0) === 0,
  );

  protected readonly errorMessage = computed(() => {
    const err = this.shoppingLists.error();
    return err ? getErrorMessage(err, this.translate) : '';
  });

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(1)],
    }),
    householdId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  protected readonly submitState = signal<SubmitState>({ kind: 'idle' });

  protected onSubmit(): void {
    if (this.form.invalid) return;
    this.submitState.set({ kind: 'submitting' });

    const { name, householdId } = this.form.getRawValue();
    this.client
      .create({ name, householdId })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.form.reset();
          this.submitState.set({ kind: 'idle' });
          this._refresh.update(n => n + 1);
          this.toast.show('success', this.translate.instant('shoppingLists.listCreated'));
        },
        error: (err: unknown) => {
          this.submitState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed to create') });
        },
      });
  }
}
