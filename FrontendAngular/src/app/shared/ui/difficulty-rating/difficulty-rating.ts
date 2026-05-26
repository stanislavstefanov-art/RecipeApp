import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-difficulty-rating',
  templateUrl: './difficulty-rating.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DifficultyRatingComponent {
  readonly value = input<number | null>(null);
  readonly readonly = input<boolean>(false);
  readonly size = input<'sm' | 'md'>('md');
  readonly changed = output<number | null>();

  protected readonly levels = [1, 2, 3] as const;

  protected isFilled(n: number): boolean {
    const v = this.value();
    return v != null && n <= v;
  }

  protected onSelect(n: number): void {
    if (!this.readonly()) {
      this.changed.emit(this.value() === n ? null : n);
    }
  }
}
