import { ChangeDetectionStrategy, Component, OnInit, inject, output } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

export interface LogCookingValue {
  readonly cookedOn: string;
  readonly servings: number;
  readonly notes: string | null;
}

function noFutureDate(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  const today = new Date().toISOString().slice(0, 10);
  return control.value > today ? { futureDate: true } : null;
}

@Component({
  selector: 'app-log-cooking-form',
  imports: [ReactiveFormsModule, TranslateModule],
  templateUrl: './log-cooking-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LogCookingFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  readonly submitted = output<LogCookingValue>();

  protected readonly form = this.fb.nonNullable.group({
    cookedOn: ['', [Validators.required, noFutureDate]],
    servings: [1, [Validators.required, Validators.min(1), Validators.max(100)]],
    notes: [''],
  });

  ngOnInit(): void {
    this.form.patchValue({ cookedOn: new Date().toISOString().slice(0, 10) });
  }

  protected onSubmit(): void {
    if (this.form.invalid) return;
    const { cookedOn, servings, notes } = this.form.getRawValue();
    this.submitted.emit({ cookedOn, servings, notes: notes.trim() || null });
    this.form.patchValue({ cookedOn: new Date().toISOString().slice(0, 10), servings: 1, notes: '' });
  }

  protected get todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }
}
