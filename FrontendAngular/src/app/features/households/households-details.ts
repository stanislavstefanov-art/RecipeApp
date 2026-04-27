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
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { HouseholdsClient } from '../../api/households.client';
import { PersonsClient } from '../../api/persons.client';
import { getErrorMessage } from '../../shared/get-error-message';

type AddMemberState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'error'; readonly message: string };

@Component({
  selector: 'app-households-details',
  imports: [ReactiveFormsModule, RouterLink, TranslateModule],
  templateUrl: './households-details.html',
  styleUrl: './households-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HouseholdsDetails {
  private readonly householdsClient = inject(HouseholdsClient);
  private readonly personsClient = inject(PersonsClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

  readonly id = input.required<string>();

  protected readonly household = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.householdsClient.get(params),
  });

  protected readonly persons = rxResource({
    stream: () => this.personsClient.list(),
  });

  protected readonly is404 = computed(() => {
    const err = this.household.error();
    return err instanceof HttpErrorResponse && err.status === 404;
  });

  protected readonly householdErrorMessage = computed(() => {
    const err = this.household.error();
    if (!err || (err instanceof HttpErrorResponse && err.status === 404)) {
      return '';
    }
    return getErrorMessage(err, this.translate);
  });

  protected readonly selectedPersonId = new FormControl('', { nonNullable: true });

  private readonly addMemberState = signal<AddMemberState>({ kind: 'idle' });

  protected readonly isSubmitting = computed(() => this.addMemberState().kind === 'submitting');

  protected readonly addMemberError = computed(() => {
    const state = this.addMemberState();
    return state.kind === 'error' ? state.message : '';
  });

  protected onAddMember(): void {
    const personId = this.selectedPersonId.value;
    if (!personId) {
      return;
    }

    this.addMemberState.set({ kind: 'submitting' });

    this.householdsClient
      .addMember(this.id(), personId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.addMemberState.set({ kind: 'idle' });
          this.selectedPersonId.reset();
          this.household.reload();
        },
        error: (err: unknown) => {
          this.addMemberState.set({ kind: 'error', message: this.toMessage(err) });
        },
      });
  }

  private toMessage(err: unknown): string {
    return getErrorMessage(err, this.translate, 'Failed to add member.');
  }
}
