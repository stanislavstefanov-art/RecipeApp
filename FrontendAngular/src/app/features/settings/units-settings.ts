import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { UnitsClient } from '../../api/units.client';
import { getErrorMessage } from '../../shared/get-error-message';

type ActionState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'busy' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-units-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslateModule],
  templateUrl: './units-settings.html',
})
export class UnitsSettings {
  private readonly client = inject(UnitsClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

  private readonly _refresh = signal(0);

  protected readonly units = rxResource({
    params: () => ({ r: this._refresh() }),
    stream: () => this.client.list(),
  });

  protected readonly addForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    abbreviation: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(20)] }),
  });

  protected readonly addState = signal<ActionState>({ kind: 'idle' });
  protected readonly deleteState = signal<ActionState>({ kind: 'idle' });

  protected onAdd(): void {
    if (this.addForm.invalid) return;
    const { name, abbreviation } = this.addForm.getRawValue();
    this.addState.set({ kind: 'busy' });

    this.client
      .create({ name, abbreviation })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.addForm.reset();
          this.addState.set({ kind: 'idle' });
          this._refresh.update(n => n + 1);
        },
        error: (err: unknown) => {
          this.addState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed to add unit') });
        },
      });
  }

  protected onDelete(id: string, name: string): void {
    const confirmed = window.confirm(this.translate.instant('settings.units.confirmDelete', { name }));
    if (!confirmed) return;

    this.deleteState.set({ kind: 'busy' });
    this.client
      .delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deleteState.set({ kind: 'idle' });
          this._refresh.update(n => n + 1);
        },
        error: (err: unknown) => {
          this.deleteState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed to delete unit') });
        },
      });
  }
}
