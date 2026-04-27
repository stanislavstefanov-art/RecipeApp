import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { ToastService } from '../../core/toast.service';
import { ShoppingListsClient } from '../../api/shopping-lists.client';
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
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

  protected readonly shoppingLists = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly isEmpty = computed(
    () => !this.shoppingLists.isLoading() && (this.shoppingLists.value()?.length ?? 0) === 0,
  );

  protected readonly errorMessage = computed(() => {
    const err = this.shoppingLists.error();
    return err ? getErrorMessage(err, this.translate) : '';
  });

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
          this.toast.show('success', this.translate.instant('shoppingLists.listCreated'));
        },
        error: (err: unknown) => {
          this.submitState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed to create') });
        },
      });
  }
}
