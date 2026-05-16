import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, rxResource } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { PersonsClient } from '../../api/persons.client';
import { getErrorMessage } from '../../shared/get-error-message';

type DeleteState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'deleting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-persons-details',
  imports: [RouterLink, TranslateModule],
  templateUrl: './persons-details.html',
  styleUrl: './persons-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonsDetails {
  private readonly client = inject(PersonsClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

  readonly id = input.required<string>();

  protected readonly person = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.client.get(params),
  });

  protected readonly is404 = computed(() => {
    const err = this.person.error();
    return err instanceof HttpErrorResponse && err.status === 404;
  });

  protected readonly errorMessage = computed(() => {
    const err = this.person.error();
    if (!err || this.is404()) {
      return '';
    }
    return getErrorMessage(err, this.translate);
  });

  private readonly deleteState = signal<DeleteState>({ kind: 'idle' });

  protected readonly isDeleting = computed(() => this.deleteState().kind === 'deleting');
  protected readonly deleteError = computed(() => {
    const s = this.deleteState();
    return s.kind === 'error' ? s.message : '';
  });

  protected onDelete(): void {
    const confirmed = window.confirm(this.translate.instant('persons.confirmDelete'));
    if (!confirmed) return;

    this.deleteState.set({ kind: 'deleting' });
    this.client
      .delete(this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => void this.router.navigate(['/persons']),
        error: (err: unknown) => {
          this.deleteState.set({ kind: 'error', message: getErrorMessage(err, this.translate, 'Failed to delete') });
        },
      });
  }
}
