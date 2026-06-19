import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import type { PersonDto } from '../../api/persons.dto';

export interface LogCookingValue {
  readonly cookedOn: string;
  readonly servings: number;
  readonly notes: string | null;
  readonly preparedByPersonIds: readonly string[];
}

function noFutureDate(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  return control.value > new Date().toISOString().slice(0, 10) ? { futureDate: true } : null;
}

@Component({
  selector: 'app-log-cooking-form',
  imports: [ReactiveFormsModule, TranslateModule],
  templateUrl: './log-cooking-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LogCookingFormComponent {
  private readonly fb = inject(FormBuilder);

  readonly persons = input<readonly PersonDto[]>([]);
  readonly userPersonId = input<string | null>(null);
  readonly submitted = output<LogCookingValue>();

  protected readonly selectedPersonIds = signal<Set<string>>(new Set());

  protected get todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  protected readonly form = this.fb.nonNullable.group({
    cookedOn: [this.todayIso, [Validators.required, noFutureDate]],
    servings: [1, [Validators.required, Validators.min(1), Validators.max(100)]],
    notes: [''],
  });

  protected isPersonSelected(personId: string): boolean {
    // Pre-select the linked person on first render
    if (this.selectedPersonIds().size === 0 && this.userPersonId() === personId) {
      return true;
    }
    return this.selectedPersonIds().has(personId);
  }

  protected togglePerson(personId: string, checked: boolean): void {
    const current = new Set(this.selectedPersonIds());
    // Materialise the implicit pre-selection on first interaction
    if (current.size === 0 && this.userPersonId()) {
      current.add(this.userPersonId()!);
    }
    if (checked) {
      current.add(personId);
    } else {
      current.delete(personId);
    }
    this.selectedPersonIds.set(current);
  }

  protected onSubmit(): void {
    if (this.form.invalid) return;
    const { cookedOn, servings, notes } = this.form.getRawValue();

    // Resolve selected set (accounting for implicit pre-selection)
    let ids = new Set(this.selectedPersonIds());
    if (ids.size === 0 && this.userPersonId()) {
      ids = new Set([this.userPersonId()!]);
    }

    this.submitted.emit({
      cookedOn,
      servings,
      notes: notes.trim() || null,
      preparedByPersonIds: [...ids],
    });

    this.form.patchValue({ cookedOn: this.todayIso, servings: 1, notes: '' });
    // Reset selection back to the linked person
    this.selectedPersonIds.set(new Set());
  }
}
