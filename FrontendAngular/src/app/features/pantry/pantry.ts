import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { PantryClient } from '../../api/pantry.client';
import { getErrorMessage } from '../../shared/get-error-message';

type FormState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'busy' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-pantry',
  imports: [ReactiveFormsModule, TranslateModule],
  templateUrl: './pantry.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Pantry {
  private readonly client = inject(PantryClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

  protected readonly items = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly addState = signal<FormState>({ kind: 'idle' });
  protected readonly removingId = signal<string | null>(null);

  protected readonly addForm = new FormGroup({
    ingredientName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
    notes: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] }),
  });

  protected readonly isEmpty = computed(() => {
    const v = this.items.value();
    return v !== undefined && v.length === 0;
  });

  protected readonly errorMessage = computed(() => {
    const err = this.items.error();
    return err ? getErrorMessage(err, this.translate) : '';
  });

  protected onAdd(): void {
    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }

    const { ingredientName, notes } = this.addForm.getRawValue();
    this.addState.set({ kind: 'busy' });
    this.client
      .add({ ingredientName, notes: notes || null })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.addState.set({ kind: 'idle' });
          this.addForm.reset({ ingredientName: '', notes: '' });
          this.items.reload();
        },
        error: (err: unknown) => {
          this.addState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed to add') });
        },
      });
  }

  protected onRemove(id: string): void {
    this.removingId.set(id);
    this.client
      .remove(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => { this.removingId.set(null); this.items.reload(); },
        error: () => this.removingId.set(null),
      });
  }
}
